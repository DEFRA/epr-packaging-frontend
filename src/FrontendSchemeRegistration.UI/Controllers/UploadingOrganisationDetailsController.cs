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
[Route(PagePaths.UploadingOrganisationDetails)]
public class UploadingOrganisationDetailsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public UploadingOrganisationDetailsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetails)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        if (!ValidationHasCompleted(submission) || session is null)
        {
            return GetUploadingOrganisationDetailsViewResult(submissionId);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadCompanyDetails))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (HasFileErrors(submission))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", new { submissionId = submissionId.ToString() });
        }

        if (HasRowValidationErrors(submission))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsErrors", new { submissionId = submissionId.ToString() });
        }

        return RedirectToAction("Get", "FileUploadCompanyDetailsSuccess", new { submissionId = submissionId.ToString() });
    }

    private static bool ValidationHasCompleted(RegistrationSubmission submission)
    {
        return submission.CompanyDetailsDataComplete;
    }

    private static bool HasFileErrors(RegistrationSubmission submission)
    {
        return submission.Errors.Count > 0;
    }

    private static bool HasRowValidationErrors(RegistrationSubmission submission)
    {
        return submission.RowErrorCount.HasValue && submission.RowErrorCount > 0;
    }

    private ViewResult GetUploadingOrganisationDetailsViewResult(Guid submissionId)
    {
        return View("UploadingOrganisationDetails", new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }
}