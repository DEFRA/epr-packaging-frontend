using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Controllers;

using Sessions;
[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadWarning)]
public class FileUploadWarningController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ValidationOptions _validationOptions;

    public FileUploadWarningController(
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

        if (submission is null || !submission.PomDataComplete)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is not null && session.RegistrationSession.Journey.LastOrDefault() != PagePaths.FileUploadSubLanding &&
            !session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploading))
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        return View(
            "FileUploadWarning",
            new FileUploadWarningViewModel
            {
                FileName = submission.PomFileName,
                SubmissionId = submissionId,
                MaxWarningsToProcess = _validationOptions.MaxIssuesToProcess,
                MaxReportSize = _validationOptions.MaxIssueReportSize
            });
    }

    [HttpPost]
    public async Task<IActionResult> FileUploadDecision(FileUploadWarningViewModel model)
    {
        ModelState.Remove(nameof(model.FileName));
        ModelState.Remove(nameof(model.MaxReportSize));

        if (!ModelState.IsValid)
        {
            return View("FileUploadWarning", model);
        }

        if (model.UploadNewFile.HasValue)
        {
            return model.UploadNewFile.Value ?
                RedirectToAction("Get", "FileUpload", new { submissionId = model.SubmissionId }) :
                RedirectToAction("Get", "FileUploadCheckFileAndSubmit", new { submissionId = model.SubmissionId });
        }

        return View("FileUploadWarning", model);
    }
}