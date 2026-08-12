namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;

public class PrivacyPageTests
{
    private ComponentTestContext Context { get; } = new();

    [SetUp]
    public void SetUp()
    {
        Context.SetUp();
    }

    [Test]
    public async Task Then_I_Can_Get_To_The_Privacy_Page()
    {
        var response = await Context.Client.GetAsync("/privacy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Privacy notice");
    }

    [Test]
    public async Task WhenReturnUrlIsAllowed_ShowsItAsTheBackLink()
    {
        var response = await Context.Client.GetAsync("/privacy?returnUrl=/report-data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<a href=\"/report-data\" class=\"govuk-back-link\"");
    }

    [Test]
    public async Task WhenReturnUrlIsNotAllowed_FallsBackToHomeLink()
    {
        var response = await Context.Client.GetAsync("/privacy?returnUrl=https://evil.example.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<a href=\"/\" class=\"govuk-back-link\"");
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}
