namespace FrontendSchemeRegistration.UI.Helpers;

using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

public static class FileHelpers
{
    private const int OneMB = 1048576;
    private const int OneKB = 1024;

    public static async Task<byte[]> ProcessFileAsync(
        MultipartSection section,
        string fileName,
        ModelStateDictionary modelState,
        string uploadFieldName,
        IFileUploadSize fileUploadSize,
        IFileUploadMessages fileUploadMessages)
    {
        using var memoryStream = new MemoryStream();
        await section.Body.CopyToAsync(memoryStream);

        if (string.IsNullOrEmpty(fileName))
        {
            modelState.AddModelError(uploadFieldName, fileUploadMessages.SelectACsvFile);
            return Array.Empty<byte>();
        }

        if (!IsExtensionCsv(fileName))
        {
            modelState.AddModelError(uploadFieldName, fileUploadMessages.TheSelectedFileMustBeACsv);
            return Array.Empty<byte>();
        }

        // Reset the memory stream position to the beginning, just in case it's not at the start
        memoryStream.Position = 0;

        using (var reader = new StreamReader(memoryStream, leaveOpen: true)) // Ensure memoryStream is not disposed
        {
            // Read the first line to check if there is any content
            var firstLine = await reader.ReadLineAsync();

            if (memoryStream.Length == 0 || string.IsNullOrWhiteSpace(firstLine))
            {
                modelState.AddModelError(uploadFieldName, fileUploadMessages.TheSelectedFileIsEmpty);
                return Array.Empty<byte>();
            }
        }

        if (memoryStream.Length >= fileUploadSize.FileUploadLimitInBytes)
        {
            var fileUploadLimit = fileUploadSize.FileUploadLimitInBytes;
            var sizeLimit = fileUploadLimit >= OneMB ? fileUploadLimit / OneMB : (fileUploadLimit >= OneKB ? fileUploadLimit /OneKB : fileUploadLimit);
            modelState.AddModelError(uploadFieldName, string.Format(fileUploadMessages.TheSelectedFileMustBeSmallerThan, sizeLimit));

            return Array.Empty<byte>();
        }

        return memoryStream.ToArray();
    }

    private static bool IsExtensionCsv(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".csv");
    }
}