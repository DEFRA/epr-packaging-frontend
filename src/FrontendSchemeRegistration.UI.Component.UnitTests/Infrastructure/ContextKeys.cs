namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class ContextKeys
{
    public const string ComponentTestClient = nameof(ComponentTestClient);
    public const string HttpResponse = nameof(HttpResponse);
    public const string HttpResponseContent = nameof(HttpResponseContent);
    public const string HttpResponseRedirectContent = nameof(HttpResponseRedirectContent);
    public const string TestServer = nameof(TestServer);
    public const string Environment = nameof(Environment);
    public const string UploadPageUrl = nameof(UploadPageUrl);
}