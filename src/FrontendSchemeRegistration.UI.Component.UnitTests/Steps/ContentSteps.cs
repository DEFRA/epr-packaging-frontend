namespace FrontendSchemeRegistration.UI.Component.UnitTests.Steps;

using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Infrastructure;
using Reqnroll;

[Binding]
public class ContentSteps(ScenarioContext context)
{
    [Then("the page content includes the following: (.*)")]
    public void ThenThePageContentIncludesTheFollowing(string expectedContent)
    {
        var response = context.Get<string>(ContextKeys.HttpResponseContent);
        AssertContentIncludes(response, expectedContent);
    }

    [Then("the page redirect content includes the following: (.*)")]
    public void ThenThePageRedirectContentIncludesTheFollowing(string expectedContent)
    {
        var response = context.Get<string>(ContextKeys.HttpResponseRedirectContent);
        AssertContentIncludes(response, expectedContent);
    }

    [Then("the page content contains a link to (.*)")]
    public void ThenThePageContentContainsLinkTo(string path)
    {
        var content = GetPageContent();
        AssertLinkExists(content, path);
    }

    [Then("the page redirect content contains a link to (.*)")]
    public void ThenThePageRedirectContentContainsLinkTo(string path)
    {
        var content = GetRedirectContent();
        AssertLinkExists(content, path);
    }

    [Then("the page content contains a link titled (.*) to (.*)")]
    public void ThenThePageContentContainsLinkTitledTo(string linkText, string path)
    {
        var content = GetPageContent();
        AssertLinkWithTextExists(content, linkText, path);
    }

    [Then("the page redirect content contains a link titled (.*) to (.*)")]
    public void ThenThePageRedirectContentContainsLinkTitledTo(string linkText, string path)
    {
        var content = GetRedirectContent();
        AssertLinkWithTextExists(content, linkText, path);
    }

    private static string GetPageContent(ScenarioContext ctx)
    {
        return ctx.Get<string>(ContextKeys.HttpResponseContent);
    }

    private string GetPageContent() => GetPageContent(context);

    private static string GetRedirectContent(ScenarioContext ctx)
    {
        return ctx.Get<string>(ContextKeys.HttpResponseRedirectContent);
    }

    private string GetRedirectContent() => GetRedirectContent(context);

    private static void AssertContentIncludes(string htmlContent, string expectedContent)
    {
        var normalizedContent = NormalizeForContentComparison(htmlContent);
        var normalizedExpected = NormalizeApostrophes(expectedContent);
        normalizedContent.Should().Contain(normalizedExpected,
            "page should contain '{0}'", expectedContent);
    }

    private static void AssertLinkExists(string content, string path)
    {
        var normalizedPath = path.StartsWith("/") ? path : $"/report-data/{path}";
        var linkPattern = new Regex($@"href\s*=\s*[""'][^""']*{Regex.Escape(normalizedPath)}[^""']*[""']", RegexOptions.IgnoreCase);
        linkPattern.IsMatch(content).Should().BeTrue(
            "page should contain a link to {0}. Content length: {1} chars", normalizedPath, content.Length);
    }

    private static void AssertLinkWithTextExists(string content, string linkText, string path)
    {
        var normalizedPath = path.StartsWith("/") ? path : $"/report-data/{path}";
        var normalizedContent = NormalizeForContentComparison(content);
        var normalizedLinkText = NormalizeApostrophes(linkText);
        normalizedContent.Should().Contain(normalizedLinkText,
            "page should contain link text '{0}'", linkText);
        var linkPattern = new Regex($@"href\s*=\s*[""'][^""']*{Regex.Escape(normalizedPath)}[^""']*[""']", RegexOptions.IgnoreCase);
        linkPattern.IsMatch(content).Should().BeTrue(
            "page should contain a link to {0} with text '{1}'", normalizedPath, linkText);
    }

    /// <summary>
    /// Decodes HTML entities (e.g. &#x2019;) and normalizes apostrophes for comparison.
    /// </summary>
    private static string NormalizeForContentComparison(string html)
    {
        var decoded = WebUtility.HtmlDecode(html);
        return NormalizeApostrophes(decoded);
    }

    /// <summary>
    /// Normalizes apostrophe variants (straight, curly, etc.) to a canonical form.
    /// </summary>
    private static string NormalizeApostrophes(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s
            .Replace('\u2019', '\'')  // RIGHT SINGLE QUOTATION MARK
            .Replace('\u2018', '\''); // LEFT SINGLE QUOTATION MARK
    }
}