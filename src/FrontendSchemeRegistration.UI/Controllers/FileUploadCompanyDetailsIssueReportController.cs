﻿using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadCompanyDetailsErrorReport)]
public class FileUploadCompanyDetailsIssueReportController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IErrorReportService _errorReportService;

    public FileUploadCompanyDetailsIssueReportController(ISubmissionService submissionService, IErrorReportService errorReportService)
    {
        _submissionService = submissionService;
        _errorReportService = errorReportService;
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> Get(Guid submissionId)
    {
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null || !submission.CompanyDetailsDataComplete || submission.ValidationPass)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var stream = await _errorReportService.GetRegistrationErrorReportStreamAsync(submissionId);

        if (stream is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var splitFileName = Path.GetFileNameWithoutExtension(submission.CompanyDetailsFileName);
        return File(stream, "text/csv", $"{splitFileName} error report.csv");
    }
}