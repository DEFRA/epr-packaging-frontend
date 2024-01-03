using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadErrorReport)]
public class FileUploadIssueReportController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IErrorReportService _errorReportService;

    public FileUploadIssueReportController(ISubmissionService submissionService, IErrorReportService errorReportService)
    {
        _submissionService = submissionService;
        _errorReportService = errorReportService;
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> Get(Guid submissionId)
    {
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is null
            || !submission.PomDataComplete
            || (!submission.HasWarnings && submission.ValidationPass))
        {
            return RedirectToAction("Get", "FileUpload");
        }

        var stream = await _errorReportService.GetErrorReportStreamAsync(submissionId);
        var splitFileName = Path.GetFileNameWithoutExtension(submission.PomFileName);

        if (submission.HasWarnings && submission.ValidationPass)
        {
            return File(stream, "text/csv", $"{splitFileName} warning report.csv");
        }

        return File(stream, "text/csv", $"{splitFileName} error report.csv");
    }
}