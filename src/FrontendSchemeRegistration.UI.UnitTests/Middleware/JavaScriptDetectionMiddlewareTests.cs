namespace FrontendSchemeRegistration.UI.UnitTests.Middleware;

using System.Text;
using Application.Options;
using Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using UI.Middleware;
using CookieOptions = Application.Options.CookieOptions;

[TestFixture]
public class JavaScriptDetectionMiddlewareTests
{
    private const string CookieName = ".epr_js_enabled";
    private const string VerifiedCookieName = ".epr_js_verified";
    private const string SignedOutCallbackPath = "/signout/B2C_1A_EPR_SignUpSignIn";

    private Mock<RequestDelegate> _next;
    private IOptions<CookieOptions> _cookieOptions;
    private IOptions<AzureAdB2COptions> _b2COptions;
    private JavaScriptDetectionMiddleware _middleware;

    [SetUp]
    public void SetUp()
    {
        _next = new Mock<RequestDelegate>();
        _next.Setup(d => d(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        _cookieOptions = Options.Create(new CookieOptions
        {
            JsEnabledCookieName = CookieName,
            JsVerifiedCookieName = VerifiedCookieName,
        });
        _b2COptions = Options.Create(new AzureAdB2COptions { SignedOutCallbackPath = SignedOutCallbackPath });

        _middleware = new JavaScriptDetectionMiddleware(_next.Object);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_AndDoesNotWriteGatePage_WhenJsCookiePresent()
    {
        var context = CreateContext("/some-page");
        context.Request.Headers.Cookie = $"{CookieName}=1";

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
        await AssertResponseIsEmptyAsync(context);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_WhenCookieNameIsNull()
    {
        var cookieOptions = Options.Create(new CookieOptions { JsEnabledCookieName = null! });
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
        await AssertResponseIsEmptyAsync(context);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_WhenCookieNameIsEmpty()
    {
        var cookieOptions = Options.Create(new CookieOptions { JsEnabledCookieName = string.Empty });
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
        await AssertResponseIsEmptyAsync(context);
    }

    [TestCase("/javascript-required")]
    [TestCase("/error")]
    [TestCase("/error/something")]
    [TestCase("/admin/health")]
    [TestCase("/signin-oidc")]
    [TestCase("/signout-oidc")]
    [TestCase("/signout-callback-oidc")]
    public async Task InvokeAsync_CallsNext_WhenPathIsBypassPath(string path)
    {
        var context = CreateContext(path);

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
        await AssertResponseIsEmptyAsync(context);
    }

    [TestCase("/SIGNIN-OIDC")]
    [TestCase("/Error")]
    public async Task InvokeAsync_CallsNext_WhenBypassPathMatchIsCaseInsensitive(string path)
    {
        var context = CreateContext(path);

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_WhenPathMatchesConfiguredSignedOutCallbackPath()
    {
        var context = CreateContext(SignedOutCallbackPath);

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        _next.Verify(d => d(context), Times.Once);
        await AssertResponseIsEmptyAsync(context);
    }

    [Test]
    public async Task InvokeAsync_DoesNotMatchSignedOutCallbackPath_WhenItIsEmpty()
    {
        var b2COptions = Options.Create(new AzureAdB2COptions { SignedOutCallbackPath = string.Empty });
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, b2COptions);

        _next.Verify(d => d(context), Times.Never);
        (await ReadResponseAsync(context)).Should().Contain("<!DOCTYPE html>");
    }

    [Test]
    public async Task InvokeAsync_WritesGatePage_WhenCookieMissingAndPathNotBypassed()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        _next.Verify(d => d(It.IsAny<HttpContext>()), Times.Never);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.ContentType.Should().Be("text/html; charset=utf-8");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store, no-cache, must-revalidate");
        context.Response.Headers.Pragma.ToString().Should().Be("no-cache");

        var body = await ReadResponseAsync(context);
        body.Should().StartWith("<!DOCTYPE html>");
        body.Should().Contain("<title>Loading…</title>");
        body.Should().Contain("js-detection-spinner");
    }

    [Test]
    public async Task InvokeAsync_GatePage_SetsCookieAttributesIncludingTheCookieName()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain($"{CookieName}=1");
        body.Should().Contain("path=/");
        body.Should().Contain("secure");
        body.Should().Contain("samesite=lax");
    }

    [Test]
    public async Task InvokeAsync_GatePage_SetsShortLivedVerifiedCookie()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain($"{VerifiedCookieName}=1");
        body.Should().Contain($"max-age={JavaScriptDetectionMiddleware.VerifiedCookieMaxAgeSeconds}");
    }

    [Test]
    public async Task InvokeAsync_GatePage_OmitsVerifiedCookie_WhenVerifiedCookieNameNotConfigured()
    {
        var cookieOptions = Options.Create(new CookieOptions
        {
            JsEnabledCookieName = CookieName,
            JsVerifiedCookieName = null!,
        });
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain($"{CookieName}=1");
        body.Should().NotContain("max-age=");
    }

    [Test]
    public async Task InvokeAsync_GatePage_UsesPathBaseWhenPresent()
    {
        var context = CreateContext("/some-page", pathBase: "/report-data");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain("href=\"/report-data/css/application.css\"");
        body.Should().Contain("url=/report-data/javascript-required");
        body.Should().Contain("path=/report-data");
    }

    [Test]
    public async Task InvokeAsync_GatePage_DefaultsCookiePathToSlashWhenNoPathBase()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain("href=\"/css/application.css\"");
        body.Should().Contain("url=/javascript-required");
        body.Should().Contain("path=/;");
    }

    [Test]
    public async Task InvokeAsync_GatePage_EmbedsScriptNonceFromHttpContext()
    {
        const string nonce = "test-nonce-abc123";
        var context = CreateContext("/some-page");
        context.Items[ContextKeys.ScriptNonceKey] = nonce;

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain($"<script nonce=\"{nonce}\">");
    }

    [Test]
    public async Task InvokeAsync_GatePage_UsesEmptyNonceWhenNoneInHttpContext()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain("<script nonce=\"\">");
    }

    [Test]
    public async Task InvokeAsync_GatePage_IncludesNoScriptRedirect()
    {
        var context = CreateContext("/some-page");

        await _middleware.InvokeAsync(context, _cookieOptions, _b2COptions);

        var body = await ReadResponseAsync(context);
        body.Should().Contain("<noscript><meta http-equiv=\"refresh\" content=\"0; url=/javascript-required\"></noscript>");
    }

    private static DefaultHttpContext CreateContext(string path, string? pathBase = null)
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            Request =
            {
                Path = path
            }
        };
        if (!string.IsNullOrEmpty(pathBase))
        {
            context.Request.PathBase = pathBase;
        }
        return context;
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static async Task AssertResponseIsEmptyAsync(HttpContext context)
    {
        (await ReadResponseAsync(context)).Should().BeEmpty();
    }
}
