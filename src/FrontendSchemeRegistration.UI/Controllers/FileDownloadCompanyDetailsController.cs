﻿namespace FrontendSchemeRegistration.UI.Controllers;

using global::FrontendSchemeRegistration.Application.DTOs.Submission;
using global::FrontendSchemeRegistration.Application.Enums;
using global::FrontendSchemeRegistration.Application.Services.Interfaces;
using global::FrontendSchemeRegistration.UI.Constants;
using global::FrontendSchemeRegistration.UI.Services.Interfaces;
using global::FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

[FeatureGate(FeatureFlags.EnableCsvDownload)]
public class FileDownloadCompanyDetailsController(ISubmissionService submissionService, IFileDownloadService fileDownloadService) : Controller
{
    public async Task<IActionResult> Get(FileDownloadViewModel model)
    {
        if (!ModelState.IsValid || (model.SubmissionId == Guid.Empty))
        {
            return BadRequest();
        }

        var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(model.SubmissionId);

        Guid fileId;
        string fileName;

        if (model.Type == FileDownloadType.Upload)
        {
            fileId = submission.LastUploadedValidFiles.CompanyDetailsFileId;
            fileName = submission.LastUploadedValidFiles.CompanyDetailsFileName;
        }
        else if(model.FileId == null)
        {
            fileId = submission.LastSubmittedFiles.CompanyDetailsFileId;
            fileName = submission.LastSubmittedFiles.CompanyDetailsFileName;
        }
        else
        {
            var submissionHistory = await submissionService.GetSubmissionHistoryAsync(
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

        var fileData = await fileDownloadService.GetFileAsync(fileId, fileName, SubmissionType.Registration, model.SubmissionId);

        return File(fileData, "text/csv", fileName);
    }
}
