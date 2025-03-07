using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploading)]
public class FileUploadingController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadingController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUpload)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUpload");
        }

        if (session is null)
        {
            return GetFileUploadingViewResult(submissionId);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUpload))
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        return submission.PomDataComplete || submission.Errors.Count > 0
            ? GetNextPageAsync(submission.Id, submission.ValidationPass, submission.HasWarnings, submission.Errors.Count > 0).Result
            : GetFileUploadingViewResult(submissionId);
    }

    private async Task<RedirectToActionResult> GetNextPageAsync(Guid submissionId, bool validationPass, bool hasWarnings, bool exceptionErrorOccurred)
    {
        var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

        if (exceptionErrorOccurred)
        {
            routeValues.Add("showErrors", true);
            return RedirectToAction("Get", "FileUpload", routeValues);
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is not null)
        {
            session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploading);
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }

        if (!validationPass)
        {
            return RedirectToAction("Get", "FileUploadFailure", routeValues);
        }

        return RedirectToAction("Get", hasWarnings ? "FileUploadWarning" : "FileUploadCheckFileAndSubmit", routeValues);
    }

    private ViewResult GetFileUploadingViewResult(Guid submissionId)
    {
        return View("FileUploading", new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }
}