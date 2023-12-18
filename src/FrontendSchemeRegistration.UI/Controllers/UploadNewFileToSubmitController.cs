namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Extensions;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.UploadNewFileToSubmit)]
[IgnoreAntiforgeryToken]
public class UploadNewFileToSubmitController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IUserAccountService _userAccountService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public UploadNewFileToSubmitController(
        ISubmissionService submissionService, IUserAccountService userAccountService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _userAccountService = userAccountService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadSubLanding)]
    public async Task<IActionResult> Get()
    {
        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is not null)
        {
            var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;
            if (organisationRole is not null)
            {
                var submissionId = Guid.Parse(Request.Query["submissionId"]);
                var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);
                if (submission is not null)
                {
                    var userData = User.GetUserData();

                    var uploadedByGuid = submission.LastUploadedValidFile?.UploadedBy;
                    var submittedByGuid = submission.LastSubmittedFile?.SubmittedBy;

                    var uploadedBy = (await _userAccountService.GetPersonByUserId(uploadedByGuid.Value)).GetUserName();

                    string submittedBy = null;

                    if (uploadedByGuid.Equals(submittedByGuid))
                    {
                        submittedBy = uploadedBy;
                    }
                    else if (submittedByGuid != null)
                    {
                        submittedBy = (await _userAccountService.GetPersonByUserId(submittedByGuid.Value)).GetUserName();
                    }

                    var vm = new UploadNewFileToSubmitViewModel
                    {
                        OrganisationRole = organisationRole,
                        IsApprovedOrDelegatedUser = userData.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson,
                        SubmissionId = submission.Id,
                        UploadedFileName = submission.LastUploadedValidFile?.FileName,
                        UploadedAt = submission.LastUploadedValidFile?.FileUploadDateTime,
                        UploadedBy = uploadedBy,
                        SubmittedFileName = submission.LastSubmittedFile?.FileName,
                        SubmittedAt = submission.LastSubmittedFile?.SubmittedDateTime,
                        SubmittedBy = submittedBy,
                        HasNewFileUploaded = submission.LastUploadedValidFile?.FileUploadDateTime > submission.LastSubmittedFile?.SubmittedDateTime
                    };

                    vm.Status = vm switch
                    {
                        { SubmittedFileName: null, UploadedFileName: { } } => Status.FileUploadedButNothingSubmitted,
                        { SubmittedAt: var x, UploadedAt: var y } when x > y => Status.FileSubmitted,
                        { SubmittedAt: var x, UploadedAt: var y } when x < y => Status.FileSubmittedAndNewFileUploadedButNotSubmitted,
                        _ => Status.None
                    };

                    return View("UploadNewFileToSubmit", vm);
                }
            }
        }

        return RedirectToAction("Get", "FileUploadSubLanding");
    }
}
