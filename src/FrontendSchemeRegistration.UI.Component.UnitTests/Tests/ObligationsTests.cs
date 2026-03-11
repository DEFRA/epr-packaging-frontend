namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Extensions;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;

public class ObligationsTests
{
    private ComponentTestContext Context { get; } = new();

    [SetUp]
    public void SetUp()
    {
        Context.SetUp();
    }
    
    [Test]
    public async Task WhenPrnsArePresent_ShouldLocalizeAsExpected()
    {
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync("/report-data/view-awaiting-acceptance-alt");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}