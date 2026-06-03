namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using Controllers.FrontendSchemeRegistration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MockServer;
using MockServer.WebApi;
using WireMock.Server;

public class ComponentTestContext : IDisposable
{
    private IWireMockServer _staticMockApiServer;
    private TestServer _server;
    
    public ComponentTestClient Client { get; private set; }

    public void SetUp(
        bool overrideSession = false, 
        Dictionary<string, string?>? additionalConfig = null, 
        WebApiOptions? webApiOptions = null)
    {
        _staticMockApiServer = MockApiServer.Start(webApiOptions);
        
        var factory = new CustomWebApplicationFactory<FrontendSchemeRegistrationController>();
        factory.ConfigureTestServices = services =>
        {
            if (overrideSession)
                services.AddSingleton<ISessionStore, SessionStore>();
        };
        if (additionalConfig is not null)
            factory.AdditionalConfig = additionalConfig;
        
        var webApp = factory
            .WithWebHostBuilder(c =>
                c.UseEnvironment("ComponentTest")
                    .UseConfiguration(ConfigBuilder.GenerateConfiguration())
            );
        _server = webApp.Server;
        
        Client = new ComponentTestClient(_server);
    }
    
    public SessionStore GetSessionStore() => _server.Services.GetRequiredService<ISessionStore>() as SessionStore;

    public void Dispose()
    {
        _server.Dispose();
        _staticMockApiServer.Stop();
        _staticMockApiServer.Dispose();
        Client.Dispose();
    }
}