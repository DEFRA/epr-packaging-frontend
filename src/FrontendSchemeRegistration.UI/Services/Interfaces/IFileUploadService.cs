namespace FrontendSchemeRegistration.UI.Services.Interfaces;

using Application.Enums;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public interface IFileUploadService
{
    Task<Guid> ProcessUploadAsync(
        string? contentType,
        Stream fileStream,
        ModelStateDictionary modelState,
        IFileUploadSize fileUploadSize,
        FileUploadSubmissionDetails submissionDetails);

    Task<Guid> ProcessUploadAsync(
        string? contentType,
        Stream fileStream,
        ModelStateDictionary modelState,
        Guid? submissionId,
        SubmissionType submissionType,
        IFileUploadMessages fileUploadMessages,
        IFileUploadSize fileUploadSize,
        Guid? complianceSchemeId = null);
}

