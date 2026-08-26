namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
    private const string MissingPagePath = "/report-data/no-such-page";

    private ComponentTestContext Context { get; } = new();

    [TearDown]
    public void TearDown() => Context.Dispose();

    [Test]
    public async Task WhenPathDoesNotExist_ShouldRenderPageNotFoundAndKeepThe404StatusCode()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(MissingPagePath);

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

        var response = await Context.Client.GetAsync(MissingPagePath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tudalen heb ei ganfod");

        // A missing resource makes IViewLocalizer echo the key, so this would silently pass on English fallback.
        content.Should().NotContain("page_not_found_title");
    }

    /// <summary>
    /// The language switcher built its return URL from the live request, which the status code
    /// re-execute had already rewritten to /error. Switching language therefore navigated to the error
    /// handler directly - no re-execute feature, a 200 response - and the user got "Something has gone
    /// wrong" from then on, in both languages.
    /// </summary>
    [Test]
    public async Task WhenPathDoesNotExist_LanguageSwitcherShouldReturnToTheRequestedPathNotTheErrorHandler()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(MissingPagePath);
        var welshLink = ExtractCultureLink(await response.Content.ReadAsStringAsync(), Language.Welsh);

        welshLink.Should().Contain("returnUrl=~%2Fno-such-page");
        welshLink.Should().NotContain("error");
    }

    [Test]
    public async Task WhenSwitchingLanguageOnPageNotFound_ShouldStayOnPageNotFoundInEitherLanguage()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var english = await Context.Client.GetAsync(MissingPagePath);
        var toWelsh = ExtractCultureLink(await english.Content.ReadAsStringAsync(), Language.Welsh);

        var welsh = await FollowRedirect(toWelsh);
        welsh.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var welshContent = await welsh.Content.ReadAsStringAsync();
        welshContent.Should().Contain("Tudalen heb ei ganfod");
        welshContent.Should().NotContain("Mae rhywbeth wedi mynd o'i le");

        // Switching back must not strand the user on the error handler either.
        var backToEnglish = await FollowRedirect(ExtractCultureLink(welshContent, Language.English));
        backToEnglish.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var englishContent = await backToEnglish.Content.ReadAsStringAsync();
        englishContent.Should().Contain("Page not found");
        englishContent.Should().NotContain("Something has gone wrong");
    }

    /// <summary>
    /// /error is reached by a plain GET from the app's own failure redirects, so it has to keep serving the
    /// service error page - but it was serving it as a 200 OK, reporting a failure as a success.
    /// </summary>
    [Test]
    public async Task WhenErrorPathIsRequestedDirectly_ShouldServeTheServiceErrorPageAsA500()
    {
        Context.SetUp(overrideSession: true);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync("/report-data/error");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Something has gone wrong");

        // The 500 must not send UseStatusCodePagesWithReExecute round again and stack a second page
        // into the response, nor downgrade the page to Page not found.
        content.Should().NotContain("Page not found");
        Regex.Matches(content, "<html", RegexOptions.IgnoreCase).Count.Should().Be(1);
    }

    private async Task<HttpResponseMessage> FollowRedirect(string url)
    {
        var redirect = await Context.Client.GetAsync(url);
        redirect.StatusCode.Should().Be(HttpStatusCode.Redirect);

        return await Context.Client.GetAsync(redirect.Headers.Location!.ToString());
    }

    private static string ExtractCultureLink(string html, string culture)
    {
        // Matched on the generated href rather than the link text, which is localised.
        var link = Regex.Matches(html, "href=\"(?<href>[^\"]*/culture\\?[^\"]*)\"", RegexOptions.IgnoreCase)
            .Select(match => WebUtility.HtmlDecode(match.Groups["href"].Value))
            .FirstOrDefault(href => href.Contains($"culture={culture}", StringComparison.OrdinalIgnoreCase));

        link.Should().NotBeNull($"the language switcher should offer a link to '{culture}'");

        return link!;
    }
}
