namespace FrontendSchemeRegistration.UI.ComponentTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Controllers.FrontendSchemeRegistration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MockServer;
using Reqnroll;
using WireMock.Server;

[ExcludeFromCodeCoverage]
[Binding]
public class TestEnvironmentManagement(ScenarioContext context)
{
    private IWireMockServer _staticMockApiServer;
    private TestServer _server;
    private ITestHttpClient _componentTestClient;
    private readonly string _environmentBaseUrl = "{SOME_TEST_URL}";

    [BeforeScenario("WireMockServer")]
    public void StartWebApp()
    {
        _staticMockApiServer = MockApiServer.Start();
        var webApp = new CustomWebApplicationFactory<FrontendSchemeRegistrationController>()
            .WithWebHostBuilder(c =>
                c.UseEnvironment("ComponentTest").UseConfiguration(ConfigBuilder.GenerateConfiguration()));
        _server = webApp.Server;
        
        _componentTestClient = new ComponentTestClient(_server);

        context.Set(_server, ContextKeys.TestServer);
        context.Set(_componentTestClient, ContextKeys.ComponentTestClient);
    }

    [BeforeScenario("RunOnDev")]
    public void SetupRunOnDev()
    {
        _componentTestClient = new EnvironmentTestClient(_environmentBaseUrl);
        context.Set<TestServer>(null, ContextKeys.TestServer);
        context.Set(_environmentBaseUrl, ContextKeys.Environment);
        context.Set(_componentTestClient, ContextKeys.ComponentTestClient);
    }

    [AfterScenario("WireMockServer")]
    public void StopEnvironment()
    {
        _server.Dispose();
        _staticMockApiServer.Stop();
        _staticMockApiServer.Dispose();
        _componentTestClient.Dispose();
    }

    [BeforeScenario("AuthenticateUser")]
    public async Task AuthenticateUser()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        
        var formData = new Dictionary<string, string> { { "Email", "test@test.com" }, { "UserId", "9e4da0ed-cdff-44a1-8ae0-cef7f22b914b" }, {"ReturnUrl","/home"} };
        
        await client.PostAsync("/services/account-details", formData);
    }

    [BeforeScenario("AuthenticateUserDev")]
    public async Task AuthenticateUserDev()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var redirection = await client.GetAsync("/report-data/");
        var redirectUrl = redirection.Headers.Location?.ToString() ?? redirection.RequestMessage.RequestUri.ToString();
        
        var formData = new Dictionary<string, string> { { "Email", "test@test.com" }, { "Password", "{SOME_PASSWORD}" } };
        
        await client.PostAsync(redirectUrl, formData);
    }
}