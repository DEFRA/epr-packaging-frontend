namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public abstract class TestClientBase : ITestHttpClient
{
    public abstract void Dispose();

    public abstract Task<HttpResponseMessage> GetAsync(string url);

    public abstract Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> content);

    public abstract Task<HttpResponseMessage> PostWithFileAsync(string url, byte[] fileContent, string fileName, Dictionary<string, string>? additionalFormData = null);
    
    protected static string ExtractRequestVerificationToken(string html)
    {
        var markerIdx = html.IndexOf("__RequestVerificationToken", StringComparison.OrdinalIgnoreCase);
        if (markerIdx < 0)
            return string.Empty;

        var tagStart = html.LastIndexOf('<', markerIdx);
        if (tagStart < 0)
            tagStart = 0;
        var tagEnd = html.IndexOf('>', markerIdx);
        if (tagEnd < 0)
            tagEnd = html.Length;
        var tagContent = html.Substring(tagStart, tagEnd - tagStart);

        const string valuePrefix = "value=\"";
        var valueIdx = tagContent.IndexOf(valuePrefix, StringComparison.OrdinalIgnoreCase);
        if (valueIdx < 0)
            return string.Empty;

        var startIndex = valueIdx + valuePrefix.Length;
        if (startIndex >= tagContent.Length)
            return string.Empty;

        var endIndex = tagContent.IndexOf('"', startIndex);
        if (endIndex < 0 || endIndex <= startIndex)
            return string.Empty;

        return tagContent.Substring(startIndex, endIndex - startIndex);
    }
}