namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<FileUploadCheckFileAndSubmitController> _logger;

    public FileUploadCheckFileAndSubmitController(
        ISubmissionService submissionService,
        IUserAccountService userAccountService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<FileUploadCheckFileAndSubmitController> logger)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _userAccountService = userAccountService;
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
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction(nameof(Get), routeValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildModel(submission, userData);
            return View("FileUploadCheckFileAndSubmit", viewModel);
        }

        if (model.Submit.Value)
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

            if (userData.Organisations.FirstOrDefault() is not { OrganisationRole: OrganisationRoles.ComplianceScheme })
            {
                _sessionManager.UpdateSessionAsync(HttpContext.Session, x => x.RegistrationSession.FileId = submission.LastUploadedValidFile.FileId);
                return RedirectToAction("Get", "FileUploadSubmissionDeclaration", routeValues);
            }

            try
            {
                await _submissionService.SubmitAsync(submission.Id, model.LastValidFileId.Value);
                return RedirectToAction("Get", "FileUploadSubmissionConfirmation", routeValues);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "An error occurred when submitting submission with id: {submissionId}", submission.Id);
                return RedirectToAction("Get", "FileUploadSubmissionError", routeValues);
            }
        }

        return RedirectToAction("Get", "FileUploadSubLanding");
    }

    private async Task<FileUploadCheckFileAndSubmitViewModel> BuildModel(PomSubmission submission, UserData userData)
    {
        var organisation = userData.Organisations.First();
        var uploadedByUserId = submission.LastUploadedValidFile.UploadedBy;
        var uploadedByUserName = await GetUserNameFromId(uploadedByUserId);
        var model = new FileUploadCheckFileAndSubmitViewModel
        {
            OrganisationRole = organisation.OrganisationRole,
            SubmissionId = submission.Id,
            UserCanSubmit = userData.CanSubmit(),
            LastValidFileId = submission.LastUploadedValidFile.FileId,
            LastValidFileName = submission.LastUploadedValidFile.FileName,
            LastValidFileUploadedBy = uploadedByUserName,
            LastValidFileUploadDateTime = submission.LastUploadedValidFile.FileUploadDateTime,
            SubmittedFileName = submission.LastSubmittedFile?.FileName,
            SubmittedDateTime = submission.LastSubmittedFile?.SubmittedDateTime
        };

        var submittedByUserId = submission.LastSubmittedFile?.SubmittedBy;
        if (submission.IsSubmitted && submittedByUserId is not null)
        {
            model.SubmittedBy = uploadedByUserId == submittedByUserId
                ? uploadedByUserName
                : await GetUserNameFromId(submittedByUserId.Value);
        }

        return model;
    }

    private async Task<string> GetUserNameFromId(Guid userId)
    {
        var user = await _userAccountService.GetPersonByUserId(userId);
        return $"{user.FirstName} {user.LastName}";
    }
}