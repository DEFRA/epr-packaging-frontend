using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;
using FrontendSchemeRegistration.MockServer.WebApi;
using WireMock.Logging;

namespace FrontendSchemeRegistration.MockServer;

[ExcludeFromCodeCoverage]
public static class MockApiServer
{
    public static IWireMockServer Start(WebApiOptions? webApiOptions = null)
    {
        // WithBodyFromFile resolves relative to CWD, so set it to the
        // assembly output directory where response files are copied.
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        Directory.SetCurrentDirectory(assemblyDir);

        var settings = new WireMockServerSettings
        {
            Port = 9091,
            Logger = new WireMockConsoleLogger()
        };

        var server = StandAloneApp.Start(settings)
            .WithWebApi(webApiOptions)
            .WithPayments()
            .WithAccounts();

        return server;
    }
}