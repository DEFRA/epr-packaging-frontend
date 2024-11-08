namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Extensions;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.UploadNewFileToSubmit)]
public class UploadNewFileToSubmitController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IUserAccountService _userAccountService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IFeatureManager _featureManager;

    public UploadNewFileToSubmitController(
        ISubmissionService submissionService, IUserAccountService userAccountService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager, IFeatureManager featureManager)
    {
        _submissionService = submissionService;
        _userAccountService = userAccountService;
        _sessionManager = sessionManager;
        _featureManager = featureManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadSubLanding)]
    public async Task<IActionResult> Get()
    {
        bool showPoMResubmission = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission));
        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;
        if (organisationRole is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var submissionId = Request.Query.ContainsKey("submissionId")
            ? Guid.Parse(Request.Query["submissionId"])
            : Guid.Empty;

        if (submissionId == Guid.Empty)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);
        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var userData = User.GetUserData();
        var decision = new PomDecision();

        if (showPoMResubmission)
        {
            decision = await _submissionService.GetDecisionAsync<PomDecision>(null, submission.Id, Application.Enums.SubmissionType.Producer);
        }

        var uploadedByGuid = submission.LastUploadedValidFile?.UploadedBy;
        var submittedByGuid = submission.LastSubmittedFile?.SubmittedBy;

        string uploadedBy = null;
        bool isUploadedByPersonDeleted = false;
        bool isSubmittedByPersonDeleted = false;

        if (uploadedByGuid.HasValue)
        {
            // uploadedBy may have been deleted if this is a resubmit, so use the GetAllPersonByUserId which includes soft delete person records
            var person = (await _userAccountService.GetAllPersonByUserId(uploadedByGuid.Value));
            uploadedBy = person.GetUserName();
            isUploadedByPersonDeleted = person.IsDeleted;
        }

        string submittedBy = null;

        if (uploadedByGuid.Equals(submittedByGuid))
        {
            submittedBy = uploadedBy;
            isSubmittedByPersonDeleted = isUploadedByPersonDeleted;
        }
        else if (submittedByGuid != null)
        {
            var submittedByPerson = (await _userAccountService.GetAllPersonByUserId(submittedByGuid.Value));
            submittedBy = submittedByPerson.GetUserName();
            isSubmittedByPersonDeleted = submittedByPerson.IsDeleted;
        }

        var vm = new UploadNewFileToSubmitViewModel
        {
            OrganisationRole = organisationRole,
            IsApprovedOrDelegatedUser =
                userData.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson,
            SubmissionId = submission.Id,
            UploadedFileName = submission.LastUploadedValidFile?.FileName,
            UploadedAt = submission.LastUploadedValidFile?.FileUploadDateTime,
            UploadedBy = uploadedBy,
            SubmittedFileName = submission.LastSubmittedFile?.FileName,
            SubmittedAt = submission.LastSubmittedFile?.SubmittedDateTime,
            SubmittedBy = submittedBy,
            HasNewFileUploaded = submission.LastUploadedValidFile?.FileUploadDateTime >
                                    submission.LastSubmittedFile?.SubmittedDateTime,
            RegulatorComment = decision.Comments,
            RegulatorDecision = decision.Decision,
            IsResubmissionNeeded = decision.IsResubmissionRequired,
            IsSubmittedByPersonDeleted = isSubmittedByPersonDeleted,
            IsUploadByPersonDeleted = isSubmittedByPersonDeleted
        };

        if (!session.RegistrationSession.Journey.Contains(PagePaths.FileUploadSubLanding))
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        vm.Status = vm switch
        {
            { SubmittedFileName: null, UploadedFileName: not null } =>
                Status.FileUploadedButNothingSubmitted,
            { SubmittedAt: var x, UploadedAt: var y } when x > y => Status.FileSubmitted,
            { SubmittedAt: var x, UploadedAt: var y } when x < y => Status
                .FileSubmittedAndNewFileUploadedButNotSubmitted,
            _ => Status.None
        };

        return View("UploadNewFileToSubmit", vm);
    }
}