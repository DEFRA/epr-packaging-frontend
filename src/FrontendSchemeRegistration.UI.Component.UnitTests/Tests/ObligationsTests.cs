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
    
    [TestCase("/report-data/view-awaiting-acceptance-alt")]
    [TestCase("/report-data/view-awaiting-acceptance")]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000001")]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000003")]
    [TestCase("/report-data/accepted-prn/00000000-0000-0000-0000-000000000005")]
    public async Task WhenPrnsArePresent_ShouldLocalizeAsExpected(string path)
    {
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings).UseParameters(path);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}