namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.MockServer;
using FrontendSchemeRegistration.MockServer.WebApi;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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

    [BeforeScenario("RegistrationUploadSuccess")]
    public void SetRegistrationUploadSuccessScenario()
    {
        RegistrationUploadScenario.Current = RegistrationUploadScenario.Success;
        RegistrationUploadScenario.LockedByHook = true;
    }

    [BeforeScenario("RegistrationUploadWarnings")]
    public void SetRegistrationUploadWarningsScenario()
    {
        RegistrationUploadScenario.Current = RegistrationUploadScenario.Warnings;
        RegistrationUploadScenario.LockedByHook = true;
    }

    [BeforeScenario("RegistrationUploadErrors")]
    public void SetRegistrationUploadErrorsScenario()
    {
        RegistrationUploadScenario.Current = RegistrationUploadScenario.Errors;
        RegistrationUploadScenario.LockedByHook = true;
    }

    [BeforeScenario("RegistrationUploadClosedLoopError")]
    public void SetRegistrationUploadClosedLoopErrorScenario()
    {
        RegistrationUploadScenario.Current = RegistrationUploadScenario.ClosedLoopErrors;
        RegistrationUploadScenario.LockedByHook = true;
    }

    [AfterScenario("RegistrationUploadSuccess", "RegistrationUploadWarnings", "RegistrationUploadErrors", "RegistrationUploadClosedLoopError")]
    public void ClearRegistrationUploadScenario()
    {
        RegistrationUploadScenario.Current = null;
        RegistrationUploadScenario.LockedByHook = false;
        RegistrationUploadScenario.LastSubmissionId = null;
    }

    [BeforeScenario("PomUploadSuccess")]
    public void SetPomUploadSuccessScenario()
    {
        PomUploadScenario.Current = PomUploadScenario.Success;
        PomUploadScenario.LockedByHook = true;
    }

    [BeforeScenario("PomUploadWarnings")]
    public void SetPomUploadWarningsScenario()
    {
        PomUploadScenario.Current = PomUploadScenario.Warnings;
        PomUploadScenario.LockedByHook = true;
    }

    [BeforeScenario("PomUploadErrors")]
    public void SetPomUploadErrorsScenario()
    {
        PomUploadScenario.Current = PomUploadScenario.Errors;
        PomUploadScenario.LockedByHook = true;
    }

    [AfterScenario("PomUploadSuccess", "PomUploadWarnings", "PomUploadErrors")]
    public void ClearPomUploadScenario()
    {
        PomUploadScenario.Current = null;
        PomUploadScenario.LockedByHook = false;
        PomUploadScenario.LastSubmissionId = null;
    }

    [AfterScenario("WireMockServer")]
    public void StopEnvironment()
    {
        _server?.Dispose();
        _staticMockApiServer?.Stop();
        _staticMockApiServer?.Dispose();
        _componentTestClient?.Dispose();
    }

    [BeforeScenario("AuthenticateComplianceSchemeUser")]
    public async Task AuthenticateComplianceSchemeUser()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await AuthenticateWithEmail(client, "cs@test.com");
    }

    [BeforeScenario("AuthenticateDirectProducerUser")]
    public async Task AuthenticateDirectProducerUser()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await AuthenticateWithEmail(client, "producer@test.com");
    }

    [BeforeScenario("AuthenticateDirectProducerNotStarted")]
    public async Task AuthenticateDirectProducerNotStarted()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await AuthenticateWithEmail(client, "producer.notstarted@test.com");
    }

    [BeforeScenario("AuthenticateComplianceSchemeNotStarted")]
    public async Task AuthenticateComplianceSchemeNotStarted()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await AuthenticateWithEmail(client, "cs.notstarted@test.com");
    }

    [BeforeScenario("AuthenticateCsMemberProducerUser")]
    public async Task AuthenticateCsMemberProducerUser()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await AuthenticateWithEmail(client, "csmember@test.com");
    }

    internal static async Task AuthenticateWithEmail(ITestHttpClient client, string email)
    {
        var formData = new Dictionary<string, string>
        {
            { "Email", email },
            { "ReturnUrl", "/home" }
        };

        await client.PostAsync("/services/account-details", formData);
        await FollowRedirect(client, "/report-data");
    }

    private static async Task FollowRedirect(ITestHttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null)
        {
            await client.GetAsync(response.Headers.Location.ToString());
        }
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