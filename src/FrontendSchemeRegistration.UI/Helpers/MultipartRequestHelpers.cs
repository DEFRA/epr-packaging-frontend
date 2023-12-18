namespace FrontendSchemeRegistration.UI.Helpers;

using Microsoft.Net.Http.Headers;

public static class MultipartRequestHelpers
{
    public static string GetBoundary(string contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }

    public static bool IsMultipartContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType)
               && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasFileContentDisposition(ContentDispositionHeaderValue? contentDisposition)
    {
        return contentDisposition != null && contentDisposition.DispositionType.Equals("form-data");
    }
}
