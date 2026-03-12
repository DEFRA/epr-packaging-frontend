using System.Diagnostics.CodeAnalysis;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;
using FrontendSchemeRegistration.MockServer.WebApi;
using WireMock.Logging;

namespace FrontendSchemeRegistration.MockServer;

[ExcludeFromCodeCoverage]
public static class MockApiServer
{
    public static IWireMockServer Start()
    {
        var settings = new WireMockServerSettings
        {
            Port =9091, 
            Logger = new WireMockConsoleLogger()
        };

        var server = StandAloneApp.Start(settings)
            .WithWebApi()
            .WithPayments()
            .WithAccounts();

        return server;
    }
}