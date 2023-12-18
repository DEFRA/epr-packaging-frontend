namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadErrorReport)]
public class FileUploadErrorReportController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IErrorReportService _errorReportService;

    public FileUploadErrorReportController(ISubmissionService submissionService, IErrorReportService errorReportService)
    {
        _submissionService = submissionService;
        _errorReportService = errorReportService;
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> Get(Guid submissionId)
    {
        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        if (submission is not { PomDataComplete: true, ValidationPass: false })
        {
            return RedirectToAction("Get", "FileUpload");
        }

        var stream = await _errorReportService.GetErrorReportStreamAsync(submissionId);
        var splitFileName = Path.GetFileNameWithoutExtension(submission.PomFileName);

        return File(stream, "text/csv", $"{splitFileName} error report.csv");
    }
}