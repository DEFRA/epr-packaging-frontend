namespace FrontendSchemeRegistration.UI.ComponentTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public abstract class TestClientBase : ITestHttpClient
{
    public abstract void Dispose();

    public abstract Task<HttpResponseMessage> GetAsync(string url);

    public abstract Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> content);
    
    protected static string ExtractRequestVerificationToken(string html)
    {
        const string tokenFieldName = "\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(tokenFieldName, StringComparison.CurrentCultureIgnoreCase) +
                         tokenFieldName.Length;
        var endIndex = html.IndexOf("\"", startIndex, StringComparison.CurrentCultureIgnoreCase);
        return html.Substring(startIndex, endIndex - startIndex);
    }
}