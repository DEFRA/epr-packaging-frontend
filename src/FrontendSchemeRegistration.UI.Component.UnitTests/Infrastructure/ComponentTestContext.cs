namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using Controllers.FrontendSchemeRegistration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MockServer;
using WireMock.Server;

public class ComponentTestContext : IDisposable
{
    private IWireMockServer _staticMockApiServer;
    private TestServer _server;
    
    public ComponentTestClient Client { get; private set; }

    public void SetUp()
    {
        _staticMockApiServer = MockApiServer.Start();
        var webApp = new CustomWebApplicationFactory<FrontendSchemeRegistrationController>()
            .WithWebHostBuilder(c =>
                c.UseEnvironment("ComponentTest").UseConfiguration(ConfigBuilder.GenerateConfiguration()));
        _server = webApp.Server;
        
        Client = new ComponentTestClient(_server);
    }

    public void Dispose()
    {
        _server.Dispose();
        _staticMockApiServer.Stop();
        _staticMockApiServer.Dispose();
        Client.Dispose();
    }
}