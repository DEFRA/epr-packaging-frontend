namespace FrontendSchemeRegistration.UI.Controllers;

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

        Guid fileId;
        string fileName;

        if (model.Type == FileDownloadType.Upload)
        {
            fileId = submission.LastUploadedValidFile.FileId;
            fileName = submission.LastUploadedValidFile.FileName;
        }
        else if (model.FileId == null)
        {
            fileId = submission.LastSubmittedFile.FileId;
            fileName = submission.LastSubmittedFile.FileName;
        }
        else
        {
            var submissionHistory = await _submissionService.GetSubmissionHistoryAsync(
                model.SubmissionId,
                new DateTime(submission.Created.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var searchedSubmission = submissionHistory.Where(x => x.FileId == model.FileId).FirstOrDefault();

            if (searchedSubmission == null)
            {
                return NotFound();
            }

            fileId = searchedSubmission.FileId;
            fileName = searchedSubmission.FileName;
        }

        var fileData = await _fileDownloadService.GetFileAsync(fileId, fileName, SubmissionType.Producer, model.SubmissionId);

        return File(fileData, "text/csv", fileName);
    }
}
