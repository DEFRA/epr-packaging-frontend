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
public class FileDownloadCompanyDetailsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IFileDownloadService _fileDownloadService;

    public FileDownloadCompanyDetailsController(
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

        var (userId, organisationId) = GetUserDetails();
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(model.SubmissionId);

        var fileId = submission.LastUploadedValidFiles?.CompanyDetailsFileId ?? Guid.Empty;
        var fileData = await _fileDownloadService.GetFileAsync(fileId, submission.CompanyDetailsFileName, SubmissionType.Registration, model.SubmissionId);

        return File(fileData, "text/csv", submission.CompanyDetailsFileName);
    }

    private (Guid userId, Guid organisationId) GetUserDetails()
    {
        var user = User.GetUserData();
        return (user.Id.Value, user.Organisations[0].Id.Value);
    }
}
