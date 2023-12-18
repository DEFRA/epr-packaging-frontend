namespace FrontendSchemeRegistration.UI.Helpers;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Resources.Views.FileUpload;

public static class FileHelpers
{
    public static async Task<byte[]> ProcessFileAsync(
        MultipartSection section,
        string fileName,
        ModelStateDictionary modelState,
        string uploadFieldName,
        int maxUploadSizeInBytes)
    {
        using var memoryStream = new MemoryStream();
        await section.Body.CopyToAsync(memoryStream);

        if (string.IsNullOrEmpty(fileName))
        {
            modelState.AddModelError(uploadFieldName, FileUpload.select_a_csv_file);
            return Array.Empty<byte>();
        }

        if (!IsExtensionCsv(fileName))
        {
            modelState.AddModelError(uploadFieldName, FileUpload.the_selected_file_must_be_a_csv);
            return Array.Empty<byte>();
        }

        if (memoryStream.Length == 0)
        {
            modelState.AddModelError(uploadFieldName, FileUpload.the_selected_file_is_empty);
            return Array.Empty<byte>();
        }

        if (memoryStream.Length >= maxUploadSizeInBytes)
        {
            var sizeLimitInMegabytes = maxUploadSizeInBytes / 1048576;
            modelState.AddModelError(uploadFieldName, string.Format(FileUpload.the_selected_file_must_be_smaller_than, sizeLimitInMegabytes));
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