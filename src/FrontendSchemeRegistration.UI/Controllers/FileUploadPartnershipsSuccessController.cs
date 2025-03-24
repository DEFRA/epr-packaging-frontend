namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadPartnershipsSuccess)]
public class FileUploadPartnershipsSuccessController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadPartnershipsSuccessController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadPartnerships))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        return View("FileUploadPartnershipsSuccess", new FileUploadSuccessViewModel
        {
            FileName = submission.PartnershipsFileName,
            SubmissionId = submissionId,
            IsResubmission = session.RegistrationSession.IsResubmission
        });
    }
}