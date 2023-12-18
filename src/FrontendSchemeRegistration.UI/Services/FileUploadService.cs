namespace FrontendSchemeRegistration.UI.Services;

using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using Helpers;
using Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Resources.Views.FileUpload;

public class FileUploadService : IFileUploadService
{
    private const string UploadFieldName = "file";
    private static readonly FormOptions FormOptions = new ();
    private readonly IWebApiGatewayClient _webApiGatewayClient;
    private readonly int _fileUploadLimitInBytes;

    public FileUploadService(IWebApiGatewayClient webApiGatewayClient, IOptions<GlobalVariables> globalVariables)
    {
        _webApiGatewayClient = webApiGatewayClient;
        _fileUploadLimitInBytes = globalVariables.Value.FileUploadLimitInBytes;
    }

    public async Task<Guid> ProcessUploadAsync(
        string? contentType,
        Stream fileStream,
        string submissionPeriod,
        ModelStateDictionary modelState,
        Guid? submissionId,
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        if (!MultipartRequestHelpers.IsMultipartContentType(contentType))
        {
            modelState.AddModelError(UploadFieldName, FileUpload.select_a_csv_file);
            return Guid.Empty;
        }

        var boundary = MultipartRequestHelpers.GetBoundary(contentType, FormOptions.MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, fileStream);
        var section = await reader.ReadNextSectionAsync();

        var hasContentDispositionHeader =
            ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
        if (!hasContentDispositionHeader)
        {
            modelState.AddModelError(UploadFieldName, FileUpload.file_upload_is_invalid);
            return Guid.Empty;
        }

        if (!MultipartRequestHelpers.HasFileContentDisposition(contentDisposition))
        {
            modelState.AddModelError(UploadFieldName, FileUpload.select_a_csv_file);
            return Guid.Empty;
        }

        var fileName = contentDisposition.FileName.Value;
        var fileContent = await FileHelpers.ProcessFileAsync(
            section, fileName, modelState, UploadFieldName, _fileUploadLimitInBytes);

        if (modelState.IsValid)
        {
            return await _webApiGatewayClient.UploadFileAsync(
                fileContent,
                fileName,
                submissionPeriod,
                submissionId,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId);
        }

        return Guid.Empty;
    }
}
