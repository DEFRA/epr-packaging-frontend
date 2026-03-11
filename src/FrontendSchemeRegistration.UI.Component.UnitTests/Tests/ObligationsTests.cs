namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Controllers.FrontendSchemeRegistration;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MockServer;
using NUnit.Framework;
using WireMock.Server;

public class ObligationsTests
{
    private IWireMockServer _staticMockApiServer;
    private TestServer _server;
    private ComponentTestClient _componentTestClient;

    [SetUp]
    public void SetUp()
    {
        _staticMockApiServer = MockApiServer.Start();
        var webApp = new CustomWebApplicationFactory<FrontendSchemeRegistrationController>()
            .WithWebHostBuilder(c =>
                c.UseEnvironment("ComponentTest").UseConfiguration(ConfigBuilder.GenerateConfiguration()));
        _server = webApp.Server;
        
        _componentTestClient = new ComponentTestClient(_server);
    }
    
    [Test]
    public async Task WhenPrnsArePresent_ShouldLocalizeAsExpected()
    {
        await AuthenticateUser();

        var response = await _componentTestClient.GetAsync("/report-data/view-awaiting-acceptance-alt");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings);
    }

    private async Task AuthenticateUser()
    {
        var formData = new Dictionary<string, string> { { "Email", "test@test.com" }, { "UserId", "9e4da0ed-cdff-44a1-8ae0-cef7f22b914b" }, {"ReturnUrl","/home"} };
        
        await _componentTestClient.PostAsync("/services/account-details", formData);
    }

    [TearDown]
    public void TearDown()
    {
        _server.Dispose();
        _staticMockApiServer.Stop();
        _staticMockApiServer.Dispose();
        _componentTestClient.Dispose();
    }
}