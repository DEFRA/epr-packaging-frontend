namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Extensions;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadSubmissionConfirmation)]
public class FileUploadSubmissionConfirmation : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IUserAccountService _userAccountService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadSubmissionConfirmation(
        ISubmissionService submissionService,
        IUserAccountService userAccountService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _userAccountService = userAccountService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUpload)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        var lastSubmittedFile = submission.LastSubmittedFile;

        var model = new FileUploadSubmissionConfirmationViewModel
        {
            OrganisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole,

            SubmittedAt = lastSubmittedFile.SubmittedDateTime,
            SubmittedBy = (await _userAccountService.GetPersonByUserId(lastSubmittedFile.SubmittedBy)).GetUserName()
        };

        return View(nameof(FileUploadSubmissionConfirmation), model);
    }
}
