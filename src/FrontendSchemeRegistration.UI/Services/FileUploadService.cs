namespace FrontendSchemeRegistration.UI.Services;

using Application.Enums;
using Application.Services.Interfaces;
using Messages;
using Helpers;
using Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Resources.Views.FileUpload;

public class FileUploadService(IWebApiGatewayClient webApiGatewayClient) : IFileUploadService
{
    private const string UploadFieldName = "file";
    private static readonly FormOptions FormOptions = new ();

    public async Task<Guid> ProcessUploadAsync(
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
        bool? isResubmission = null)
    {
        var fileValidationResult = await ValidateUploadAsync(contentType, fileStream, modelState);
        if (!modelState.IsValid)
        {
            return Guid.Empty;
        }

        var fileName = fileValidationResult.ContentDisposition.FileName.Value;
        var fileContent = await FileHelpers.ProcessFileAsync(
            fileValidationResult.Section, fileName, modelState, UploadFieldName, fileUploadSize, new DefaultFileUploadMessages());

        if (modelState.IsValid)
        {
            return await webApiGatewayClient.UploadFileAsync(
                fileContent,
                fileName,
                submissionPeriod,
                submissionId,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId,
                isResubmission);
        }

        return Guid.Empty;
    }

    public async Task<Guid> ProcessUploadAsync(
        string? contentType,
        Stream fileStream,
        ModelStateDictionary modelState,
        Guid? submissionId,
        SubmissionType submissionType,
        IFileUploadMessages fileUploadMessages,
        IFileUploadSize fileUploadSize,
        Guid? complianceSchemeId = null)
    {
        var fileValidationResult = await ValidateUploadAsync(contentType, fileStream, modelState);
        if (!modelState.IsValid)
        {
            return Guid.Empty;
        }

        var fileName = fileValidationResult.ContentDisposition.FileName.Value;
        var fileContent = await FileHelpers.ProcessFileAsync(
            fileValidationResult.Section, fileName, modelState, UploadFieldName, fileUploadSize, fileUploadMessages);

        if (modelState.IsValid)
        {
            return await webApiGatewayClient.UploadSubsidiaryFileAsync(
                fileContent,
                fileName,
                submissionId,
                submissionType,
                complianceSchemeId);
        }

        return Guid.Empty;
    }

    public async Task<(MultipartSection? Section, ContentDispositionHeaderValue? ContentDisposition)> ValidateUploadAsync(
        string? contentType,
        Stream fileStream,
        ModelStateDictionary modelState)
    {
        if (!MultipartRequestHelpers.IsMultipartContentType(contentType))
        {
            modelState.AddModelError(UploadFieldName, FileUpload.select_a_csv_file);
            return (null, null);
        }

        var boundary = MultipartRequestHelpers.GetBoundary(contentType, FormOptions.MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, fileStream);
        var section = await reader.ReadNextSectionAsync();

        var hasContentDispositionHeader =
            ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
        if (!hasContentDispositionHeader)
        {
            modelState.AddModelError(UploadFieldName, FileUpload.file_upload_is_invalid);
            return (null, null);
        }

        if (!MultipartRequestHelpers.HasFileContentDisposition(contentDisposition))
        {
            modelState.AddModelError(UploadFieldName, FileUpload.select_a_csv_file);
            return (null, null);
        }

        return (section, contentDisposition);
    }
}
