namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.Application.RequestModels;
using global::FrontendSchemeRegistration.UI.Constants;
using global::FrontendSchemeRegistration.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using RequestModels;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadSubmissionDeclaration)]
public class FileUploadSubmissionDeclarationController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IRegulatorService _regulatorService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<FileUploadSubmissionDeclarationController> _logger;

    public FileUploadSubmissionDeclarationController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegulatorService regulatorService,
        IFeatureManager featureManager,
        ILogger<FileUploadSubmissionDeclarationController> logger)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _regulatorService = regulatorService;
        _featureManager = featureManager;
        _logger = logger;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        if (!userData.CanSubmit())
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        if (submission.LastUploadedValidFile is null)
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submission.Id.ToString() } };
            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues);
        }

        // SUB-332: the file on offer is not the user's most recent upload, so there is nothing safe to
        // declare here. Send them back to the check-and-submit page, which explains why and offers a
        // re-upload. Guarding the Get as well stops the declaration being reached by a direct link.
        if (submission.HasNewerUnprocessedUploadThanValidFile())
        {
            var unprocessedUploadRouteValues = new RouteValueDictionary { { "submissionId", submission.Id.ToString() } };
            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", unprocessedUploadRouteValues);
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCheckFileAndSubmit}?submissionId={submissionId}");
        return View("FileUploadSubmissionDeclaration", new FileUploadSubmissionDeclarationViewModel
        {
            OrganisationName = userData.Organisations.FirstOrDefault()!.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Post(SubmissionDeclarationRequest request)
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

        if (!userData.CanSubmit())
        {
            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        // SUB-332: last line of defence before SubmitAsync. The session FileId was captured on the previous
        // page, so a newer upload may have failed validation since then - re-check against the submission
        // rather than trusting the captured id.
        if (submission.HasNewerUnprocessedUploadThanValidFile())
        {
            _logger.LogWarning(
                "Blocked declaration of submission {SubmissionId}: the upload at {UnprocessedUploadDateTime} is newer than the last valid file, so the user must upload again before declaring",
                submission.Id,
                submission.PomFileUploadDateTime);

            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues);
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var fileId = session.RegistrationSession.FileId;

        if (fileId is null)
        {
            return RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCheckFileAndSubmit}?submissionId={submission.Id}");
            return View("FileUploadSubmissionDeclaration", new FileUploadSubmissionDeclarationViewModel
            {
                OrganisationName = userData.Organisations.FirstOrDefault()!.Name
            });
        }

        try
        {
            if (submission.LastSubmittedFile != null && !session.PomResubmissionSession.IsPomResubmissionJourney)
            {
                // The submission period for small producers is stored in Cosmos as July-December, however it's *actually* January-December.
                // For the email to the regulator, it needs to be the *actual* submission period
                var actualSubmissionPeriod = await _submissionService.GetActualSubmissionPeriod(submissionId, submission.SubmissionPeriod);
                submission.ActualSubmissionPeriod = actualSubmissionPeriod;

                ResubmissionEmailRequestModel input = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

                await _regulatorService.SendRegulatorResubmissionEmail(input);
            }

            var organisationId = session.UserData.Organisations?.FirstOrDefault()?.Id;
            if (organisationId is null)
            {
                return RedirectToAction("Get", "FileUploadSubLanding");
            }

            var isAnySubmissionAcceptedForDataPeriod = await _submissionService.IsAnySubmissionAcceptedForDataPeriod(submission, organisationId.Value, session.RegistrationSession.SelectedComplianceScheme?.Id);
			await _submissionService.SubmitAsync(submission.Id, fileId.Value, request.DeclarationName, session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber, session.PomResubmissionSession.IsPomResubmissionJourney);

			if (!submission.IsSubmitted || !isAnySubmissionAcceptedForDataPeriod)
            {
                return RedirectToAction("Get", "FileUploadSubmissionConfirmation", routeValues);
            }

            return RedirectToAction("FileUploadResubmissionConfirmation", "PackagingDataResubmission", routeValues);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "An error occurred when submitting submission with id: {submissionId}", submission.Id);
            return RedirectToAction("Get", "FileUploadSubmissionError", routeValues);
        }
    }
}