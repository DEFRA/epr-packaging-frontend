namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Extensions;
using FluentAssertions;
using Infrastructure;

/// <summary>
/// The support address used to be hard-coded in three separate views. It now comes from
/// EmailAddresses:SupportEmail, so these guard against a null binding rendering an empty mailto.
/// </summary>
public class SupportEmailTests
{
    private const string SupportEmail = "eprcustomerservice@defra.gov.uk";

    private ComponentTestContext Context { get; } = new();

    [TearDown]
    public void TearDown() => Context.Dispose();

    [Test]
    public async Task JavaScriptRequiredPage_ShouldRenderTheConfiguredSupportEmailInItsBody()
    {
        Context.SetUp(overrideSession: true);

        var response = await Context.Client.GetAsync("/report-data/javascript-required");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Anchored on the page's own bilingual label so this cannot be satisfied by the footer's copy.
        var content = await response.Content.ReadAsStringAsync();
        content.Should().MatchRegex($"""Email: / Ebost: <a[^>]*href="mailto:{SupportEmail}">""");
    }

    [Test]
    public async Task Footer_ShouldRenderTheConfiguredSupportEmail()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        // The 404 page uses the default layout and carries no support email of its own,
        // so any occurrence here comes from the footer.
        var response = await Context.Client.GetAsync("/report-data/no-such-page");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"mailto:{SupportEmail}");
    }
}
