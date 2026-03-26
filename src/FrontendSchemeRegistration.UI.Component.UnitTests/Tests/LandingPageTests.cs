namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Data;
using Extensions;
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
        await Context.Client.AuthenticateDefaultUser();

        var page = Pages.GetPages().SingleOrDefault(x => x.Name.Equals("Compliance Scheme Landing Page", StringComparison.CurrentCultureIgnoreCase));;
        
        var response = await Context.Client.GetAsync(page.Url);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Account home - SUPER TEST LTD");
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}