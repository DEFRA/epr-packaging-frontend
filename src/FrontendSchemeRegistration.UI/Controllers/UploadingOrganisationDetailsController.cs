namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using global::FrontendSchemeRegistration.UI.Services;
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
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public UploadingOrganisationDetailsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _registrationApplicationService = registrationApplicationService;

    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetails)]
    public async Task<IActionResult> Get([FromQuery] Guid submissionId, [FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var registrationYear = _registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);
        
        var submissionTask = _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var sessionTask = _sessionManager.GetSessionAsync(HttpContext.Session);
        
        await Task.WhenAll(submissionTask, sessionTask);
        
        var submission = submissionTask.Result;
        var session = sessionTask.Result;

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (!ValidationHasCompleted(submission) || session is null)
        {
            return GetUploadingOrganisationDetailsViewResult(submissionId, registrationYear, registrationJourney);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadCompanyDetails))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (HasFileErrors(submission))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { submissionId = submissionId.ToString(), registrationyear = registrationYear.ToString()} : new { submissionId = submissionId.ToString() });
        }

        if (HasRowValidationErrors(submission))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsErrors", registrationYear is not null ? new { submissionId = submissionId.ToString(), registrationyear = registrationYear.ToString() } : new { submissionId = submissionId.ToString() });
        }

        if (HasRowValidationWarnings(submission)) // Add warnings
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsWarnings", registrationYear is not null ? new { submissionId = submissionId.ToString(), registrationyear = registrationYear.ToString() } : new { submissionId = submissionId.ToString() });
        }

        return RedirectToAction("Get", "FileUploadCompanyDetailsSuccess", registrationYear is not null ? new { submissionId = submissionId.ToString(), registrationyear = registrationYear.ToString() } : new { submissionId = submissionId.ToString() });
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

    private static bool HasRowValidationWarnings(RegistrationSubmission submission)
    {
        return submission.HasWarnings && submission.Errors.Count == 0;
    }

    private ViewResult GetUploadingOrganisationDetailsViewResult(Guid submissionId, int? registrationYear, RegistrationJourney? registrationJourney)
    {
        return View("UploadingOrganisationDetails", registrationYear is not null ? new FileUploadingViewModel { SubmissionId = submissionId.ToString(), RegistrationYear = registrationYear , RegistrationJourney = registrationJourney} : new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }
}