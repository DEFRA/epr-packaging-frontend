namespace FrontendSchemeRegistration.UI.Services.Interfaces;

using Application.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public interface IFileUploadService
{
    Task<Guid> ProcessUploadAsync(
        string? contentType,
        Stream fileStream,
        string submissionPeriod,
        ModelStateDictionary modelState,
        Guid? submissionId,
        SubmissionType submissionType,
        IFileUploadMessages fileUploadMessages,
        IFileUploadSize fileUploadSize,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null,
        bool? isResubmission = null);

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