namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.Application.RequestModels;
using global::FrontendSchemeRegistration.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadCheckFileAndSubmit)]
public class FileUploadCheckFileAndSubmitController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IUserAccountService _userAccountService;
    private readonly IRegulatorService _regulatorService;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<FileUploadCheckFileAndSubmitController> _logger;

    public FileUploadCheckFileAndSubmitController(
        ISubmissionService submissionService,
        IUserAccountService userAccountService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegulatorService regulatorService,
        IFeatureManager featureManager,
        ILogger<FileUploadCheckFileAndSubmitController> logger)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _userAccountService = userAccountService;
        _regulatorService = regulatorService;
        _featureManager = featureManager;
        _logger = logger;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUpload)]
    public async Task<IActionResult> Get()
    {
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(Guid.Parse(Request.Query["submissionId"]));

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        if (submission.LastSubmittedFile?.FileId == submission.LastUploadedValidFile.FileId)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();

        if (await _featureManager.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
        {
            ViewBag.BackLinkToDisplay = session.PomResubmissionSession.IsPomResubmissionJourney ? Url.Content($"/report-data/{PagePaths.ResubmissionTaskList}") : Url.Content($"~{PagePaths.FileUploadSubLanding}");
        }

        var userData = User.GetUserData();
        var viewModel = await BuildModel(submission, userData);

        return View("FileUploadCheckFileAndSubmit", viewModel);
    }

    [HttpPost]
    [SubmissionIdActionFilter(PagePaths.FileUpload)]
    public async Task<IActionResult> Post(FileUploadCheckFileAndSubmitViewModel model)
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        if (!userData.CanSubmit())
        {
            var cannotSubmitRouteValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction(nameof(Get), cannotSubmitRouteValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        if (submission.LastSubmittedFile?.FileId == submission.LastUploadedValidFile.FileId)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildModel(submission, userData);
            return View("FileUploadCheckFileAndSubmit", viewModel);
        }

        var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

        if (userData.Organisations.FirstOrDefault() is not { OrganisationRole: OrganisationRoles.ComplianceScheme })
        {
            _sessionManager.UpdateSessionAsync(HttpContext.Session, x => x.RegistrationSession.FileId = submission.LastUploadedValidFile.FileId);
            return RedirectToAction("Get", "FileUploadSubmissionDeclaration", routeValues);
        }

        try
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await _submissionService.SubmitAsync(submission.Id, model.LastValidFileId.Value);
            var resubmissionEnabled = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission));
            if (submission.LastSubmittedFile != null && resubmissionEnabled && !session.PomResubmissionSession.IsPomResubmissionJourney)
            {
                ResubmissionEmailRequestModel input = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

                await _regulatorService.SendRegulatorResubmissionEmail(input);
            }

            var organisationId = session.UserData.Organisations?.FirstOrDefault()?.Id;
            if (organisationId is null)
            {
                return RedirectToAction("Get", "FileUploadSubLanding");
            }

            var isAnySubmissionAcceptedForDataPeriod = await _submissionService.IsAnySubmissionAcceptedForDataPeriod(submission, organisationId.Value, session.RegistrationSession.SelectedComplianceScheme?.Id);

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

    private async Task<FileUploadCheckFileAndSubmitViewModel> BuildModel(PomSubmission submission, UserData userData)
    {
        var organisation = userData.Organisations[0];
        var uploadedByUserId = submission.LastUploadedValidFile.UploadedBy;
        var uploadedByUser = await GetUserNameFromId(uploadedByUserId);
        var model = new FileUploadCheckFileAndSubmitViewModel
        {
            OrganisationRole = organisation.OrganisationRole,
            SubmissionId = submission.Id,
            UserCanSubmit = userData.CanSubmit(),
            LastValidFileId = submission.LastUploadedValidFile.FileId,
            LastValidFileName = submission.LastUploadedValidFile.FileName,
            LastValidFileUploadedBy = uploadedByUser.UserName,
            LastValidFileUploadDateTime = submission.LastUploadedValidFile.FileUploadDateTime,
            SubmittedFileName = submission.LastSubmittedFile?.FileName,
            SubmittedDateTime = submission.LastSubmittedFile?.SubmittedDateTime
        };

        var submittedByUserId = submission.LastSubmittedFile?.SubmittedBy;
        if (submission.IsSubmitted && submittedByUserId is not null)
        {
            var submittedByUser = await GetUserNameFromId(submittedByUserId.Value);

            model.SubmittedBy = uploadedByUserId == submittedByUserId
                ? uploadedByUser.UserName
                : submittedByUser.UserName;

            model.IsSubmittedByUserDeleted = submittedByUser.IsDeleted;
        }

        return model;
    }

    private async Task<(string UserName, bool IsDeleted)> GetUserNameFromId(Guid userId)
    {
        var person = await _userAccountService.GetAllPersonByUserId(userId);
        return ($"{person.FirstName} {person.LastName}", person.IsDeleted);
    }
}