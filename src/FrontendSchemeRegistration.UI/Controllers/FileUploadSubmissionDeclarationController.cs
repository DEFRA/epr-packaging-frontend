namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
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
            var resubmissionEnabled = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission));
            if (submission.LastSubmittedFile != null && resubmissionEnabled)
            {
                ResubmissionEmailRequestModel input = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

                _regulatorService.SendRegulatorResubmissionEmail(input);
            }

            await _submissionService.SubmitAsync(submission.Id, fileId.Value, request.DeclarationName);
            return RedirectToAction("Get", "FileUploadSubmissionConfirmation", routeValues);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "An error occurred when submitting submission with id: {submissionId}", submission.Id);
            return RedirectToAction("Get", "FileUploadSubmissionError", routeValues);
        }
    }
}