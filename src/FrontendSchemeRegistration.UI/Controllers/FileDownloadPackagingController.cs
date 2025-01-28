namespace FrontendSchemeRegistration.UI.Controllers;

using EPR.Common.Authorization.Extensions;
using global::FrontendSchemeRegistration.Application.DTOs.Submission;
using global::FrontendSchemeRegistration.Application.Enums;
using global::FrontendSchemeRegistration.Application.Services.Interfaces;
using global::FrontendSchemeRegistration.UI.Constants;
using global::FrontendSchemeRegistration.UI.Services.Interfaces;
using global::FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

[FeatureGate(FeatureFlags.EnableCsvDownload)]
public class FileDownloadPackagingController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IFileDownloadService _fileDownloadService;

    public FileDownloadPackagingController(
        ISubmissionService submissionService,
        IFileDownloadService fileDownloadService
        )
    {
        _submissionService = submissionService;
        _fileDownloadService = fileDownloadService;
    }

    public async Task<IActionResult> Get(FileDownloadViewModel model)
    {
        if (!ModelState.IsValid || (model.SubmissionId == Guid.Empty))
        {
            return BadRequest();
        }

        var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(model.SubmissionId);

        var fileId = submission.LastUploadedValidFile.FileId;
        var fileData = await _fileDownloadService.GetFileAsync(fileId, submission.LastUploadedValidFile.FileName, SubmissionType.Producer, model.SubmissionId);

        return File(fileData, "text/csv", submission.LastUploadedValidFile.FileName);
    }
}
