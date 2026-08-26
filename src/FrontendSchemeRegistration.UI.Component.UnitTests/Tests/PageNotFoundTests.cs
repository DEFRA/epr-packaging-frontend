namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Constants;
using Extensions;
using FluentAssertions;
using Infrastructure;

/// <summary>
/// A genuine 404 previously rendered the same "there is a problem with the service" page as a 500,
/// because <c>ErrorController.HandleThrownExceptions</c> ignored the status code it was re-executed with.
/// </summary>
public class PageNotFoundTests
{
    private ComponentTestContext Context { get; } = new();

    [TearDown]
    public void TearDown() => Context.Dispose();

    [Test]
    public async Task WhenPathDoesNotExist_ShouldRenderPageNotFoundAndKeepThe404StatusCode()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync("/report-data/no-such-page");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Page not found");
        content.Should().NotContain("Something has gone wrong");
    }

    [Test]
    public async Task WhenPathDoesNotExistInWelsh_ShouldResolveTheWelshResources()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(Language.Welsh));

        var response = await Context.Client.GetAsync("/report-data/no-such-page");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tudalen heb ei ganfod");

        // A missing resource makes IViewLocalizer echo the key, so this would silently pass on English fallback.
        content.Should().NotContain("page_not_found_title");
    }
}
