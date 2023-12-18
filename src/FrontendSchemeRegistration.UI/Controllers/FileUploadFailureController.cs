namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadFailure)]
public class FileUploadFailureController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ValidationOptions _validationOptions;

    public FileUploadFailureController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<ValidationOptions> validationOptions)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _validationOptions = validationOptions.Value;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUpload)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null || !submission.PomDataComplete || submission.ValidationPass)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is not null && !session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploading))
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        return View(
            "FileUploadFailure",
            new FileUploadFailureViewModel
            {
                FileName = submission.PomFileName,
                SubmissionId = submissionId,
                MaxErrorsToProcess = _validationOptions.MaxIssuesToProcess
            });
    }
}