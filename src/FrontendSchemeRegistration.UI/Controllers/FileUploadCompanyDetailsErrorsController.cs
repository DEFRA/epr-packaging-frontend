namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using global::FrontendSchemeRegistration.UI.Extensions;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadCompanyDetailsErrors)]
[SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
[ComplianceSchemeIdActionFilter]
public class FileUploadCompanyDetailsErrorsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ILogger<FileUploadCompanyDetailsErrorsController> _logger;

    public FileUploadCompanyDetailsErrorsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<FileUploadCompanyDetailsErrorsController> logger)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var organisation = session.UserData.Organisations.FirstOrDefault();
        if (organisation?.OrganisationRole is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        RegistrationSubmission? submission = null;
        if (Guid.TryParse(Request.Query["SubmissionId"], out var submissionId))
        {
            submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        }

        if (submission is null)
        {
            _logger.LogWarning("Submission for the organisationId {OrganisationId} and submissionId {SubmissionId} is null", organisation.Id, submissionId);
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (submission.Errors.Count > 0)
        {
            ModelStateHelpers.AddFileUploadExceptionsToModelState(
                submission.Errors.Distinct().ToList(),
                ModelState);
        }

        SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission);

        return View(
            "FileUploadCompanyDetailsErrors",
            new FileUploadErrorsViewModel
            {
                SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                OrganisationRole = organisation.OrganisationRole,
                ErrorCount = submission.RowErrorCount.GetValueOrDefault(0),
                SubmissionId = submissionId
            });
    }

    private void SetBackLink(bool isFileUploadJourneyInvokedViaRegistration, bool isResubmission)
    {
        var backLink = isFileUploadJourneyInvokedViaRegistration ? $"/report-data/{PagePaths.RegistrationTaskList}" : Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");
        ViewBag.BackLinkToDisplay = backLink.AppendResubmissionFlagToQueryString(isResubmission);
    }
}