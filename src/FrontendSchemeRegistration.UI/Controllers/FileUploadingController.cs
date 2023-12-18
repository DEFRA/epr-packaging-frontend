namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
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

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUpload))
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        return submission.PomDataComplete || submission.Errors.Any()
            ? GetNextPageAsync(submission.Id, submission.ValidationPass, submission.Errors.Any()).Result
            : View("FileUploading", new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }

    private async Task<RedirectToActionResult> GetNextPageAsync(Guid submissionId, bool validationPass, bool exceptionErrorOccurred)
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

        return validationPass
            ? RedirectToAction("Get", "FileUploadCheckFileAndSubmit", routeValues)
            : RedirectToAction("Get", "FileUploadFailure", routeValues);
    }
}