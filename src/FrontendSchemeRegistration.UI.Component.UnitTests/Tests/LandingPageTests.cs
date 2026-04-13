namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Data;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;

public class LandingPageTests
{
    private ComponentTestContext Context { get; } = new();

    [SetUp]
    public void SetUp()
    {
        Context.SetUp();
    }
    
    [Test]
    public async Task Then_I_Can_Get_To_The_Landing_Page()
    {
        await AuthenticateAsComplianceSchemeUser();

        var page = Pages.GetPages().SingleOrDefault(x => x.Name.Equals("Compliance Scheme Landing Page", StringComparison.CurrentCultureIgnoreCase));;
        
        var response = await Context.Client.GetAsync(page.Url);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Account home - COMPLIANCE SCHEME LTD");
    }

    private async Task AuthenticateAsComplianceSchemeUser()
    {
        var formData = new Dictionary<string, string>
        {
            { "Email", "cs@test.com" },
            { "UserId", "9e4da0ed-cdff-44a1-8ae0-cef7f22b914b" },
            { "ReturnUrl", "/home" }
        };
        await Context.Client.PostAsync("/services/account-details", formData);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}