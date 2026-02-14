namespace FrontendSchemeRegistration.UI.ComponentTests.Tests;

using System.Net;
using Controllers.FrontendSchemeRegistration;
using Data;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MockServer;
using NUnit.Framework;
using WireMock.Server;

public class LandingPageTests
{
    private IWireMockServer _staticMockApiServer;
    private TestServer _server;
    private ComponentTestClient _componentTestClient
        ;

    [SetUp]
    public void Arrange()
    {
        _staticMockApiServer = MockApiServer.Start();
        var webApp = new CustomWebApplicationFactory<FrontendSchemeRegistrationController>()
            .WithWebHostBuilder(c =>
                c.UseEnvironment("ComponentTest").UseConfiguration(ConfigBuilder.GenerateConfiguration()));
        _server = webApp.Server;
        
        _componentTestClient = new ComponentTestClient(_server);
    }
    
    [Test]
    public async Task Then_I_Can_Get_To_The_Landing_Page()
    {
        await AuthenticateUser();

        var page = Pages.GetPages().SingleOrDefault(x => x.Name.Equals("Compliance Scheme Landing Page", StringComparison.CurrentCultureIgnoreCase));;
        
        var response = await _componentTestClient.GetAsync(page.Url);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Account home - SUPER TEST LTD");
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