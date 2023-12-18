namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Options;

using CookieOptions = Options.CookieOptions;

[TestFixture]
public class CookieServiceTests
{
    private const string CookieName = ".epr_cookies_policy";
    private const string GoogleAnalyticsDefaultCookieName = "_ga";

    private CookieService _systemUnderTest = null!;
    private Mock<IOptions<CookieOptions>> _cookieOptions = null!;
    private Mock<IOptions<GoogleAnalyticsOptions>> _googleAnalyticsOptions = null!;
    private Mock<ILogger<CookieService>> _loggerMock = null!;

    [SetUp]
    public void TestInitialize()
    {
        _cookieOptions = new Mock<IOptions<CookieOptions>>();
        _loggerMock = new Mock<ILogger<CookieService>>();
    }

    [Test]
    public void SetCookieAcceptance_LogsError_WhenArgumentNullExceptionThrow()
    {
        // Arrange
        const string expectedLog = "Error setting cookie acceptance to 'True'";
        var requestCookieCollection = MockRequestCookieCollection("test", "test");
        HttpContext context = new DefaultHttpContext();
        MockService();

        // Act
        var act = () => _systemUnderTest.SetCookieAcceptance(true, requestCookieCollection, context.Response.Cookies);

        // Assert
        act.Should().Throw<ArgumentNullException>();
        _loggerMock.VerifyLog(logger => logger.LogError(expectedLog), Times.Once);
    }

    [Test]
    public void SetCookieAcceptance_True_ReturnValidCookie()
    {
        var requestCookieCollection = MockRequestCookieCollection();
        var context = new DefaultHttpContext();
        MockService(CookieName);

        _systemUnderTest.SetCookieAcceptance(true, requestCookieCollection, context.Response.Cookies);

        var cookieValue = GetCookieValueFromResponse(context.Response, CookieName);
        cookieValue.Should().Be("True");
    }

    [Test]
    public void SetCookieAcceptance_False_ReturnValidCookie()
    {
        var requestCookieCollection = MockRequestCookieCollection();
        var context = new DefaultHttpContext();
        MockService(CookieName);

        _systemUnderTest.SetCookieAcceptance(false, requestCookieCollection, context.Response.Cookies);

        var cookieValue = GetCookieValueFromResponse(context.Response, CookieName);
        cookieValue.Should().Be("False");
    }

    [Test]
    public void SetCookieAcceptance_False_ResetsGACookie()
    {
        var requestCookieCollection = MockRequestCookieCollection(GoogleAnalyticsDefaultCookieName, "1234");
        var context = new DefaultHttpContext();
        MockService(CookieName);

        _systemUnderTest.SetCookieAcceptance(false, requestCookieCollection, context.Response.Cookies);

        var cookieValue = GetCookieValueFromResponse(context.Response, GoogleAnalyticsDefaultCookieName);
        cookieValue.Should().Be("1234");
    }

    [Test]
    public void HasUserAcceptedCookies_LogsError_WhenArgumentNullExceptionThrow()
    {
        // Arrange
        const string expectedLog = "Error reading cookie acceptance";
        var requestCookieCollection = MockRequestCookieCollection("test", "test");
        MockService();

        // Act
        var act = () => _systemUnderTest.HasUserAcceptedCookies(requestCookieCollection);

        // Assert
        act.Should().Throw<ArgumentNullException>();
        _loggerMock.VerifyLog(logger => logger.LogError(expectedLog), Times.Once);
    }

    [Test]
    public void HasUserAcceptedCookies_True_ReturnsValidValue()
    {
        var requestCookieCollection = MockRequestCookieCollection(CookieName, "True");
        MockService(CookieName);

        var result = _systemUnderTest.HasUserAcceptedCookies(requestCookieCollection);

        result.Should().BeTrue();
    }

    [Test]
    public void HasUserAcceptedCookies_False_ReturnsValidValue()
    {
        var requestCookieCollection = MockRequestCookieCollection(CookieName, "False");
        MockService(CookieName);

        var result = _systemUnderTest.HasUserAcceptedCookies(requestCookieCollection);

        result.Should().BeFalse();
    }

    [Test]
    public void HasUserAcceptedCookies_NoCookie_ReturnsValidValue()
    {
        var requestCookieCollection = MockRequestCookieCollection("test", "test");
        MockService(CookieName);

        var result = _systemUnderTest.HasUserAcceptedCookies(requestCookieCollection);

        result.Should().BeFalse();
    }

    private static IRequestCookieCollection MockRequestCookieCollection(string key = "", string value = "")
    {
        var requestFeature = new HttpRequestFeature();
        var featureCollection = new FeatureCollection();
        requestFeature.Headers = new HeaderDictionary();
        if (key != string.Empty && value != string.Empty)
        {
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(key + "=" + value));
        }

        featureCollection.Set<IHttpRequestFeature>(requestFeature);
        var cookiesFeature = new RequestCookiesFeature(featureCollection);
        return cookiesFeature.Cookies;
    }

    private static string? GetCookieValueFromResponse(HttpResponse response, string cookieName)
    {
        foreach (var headers in response.Headers)
        {
            if (headers.Key != "Set-Cookie")
            {
                continue;
            }

            string header = headers.Value;
            if (!header.StartsWith($"{cookieName}="))
            {
                continue;
            }

            var p1 = header.IndexOf('=');
            var p2 = header.IndexOf(';');
            return header.Substring(p1 + 1, p2 - p1 - 1);
        }

        return null;
    }

    private void MockService(string? cookieName = null)
    {
        var eprCookieOptions = new CookieOptions();
        if (cookieName != null)
        {
            eprCookieOptions.CookiePolicyCookieName = cookieName;
        }

        var googleAnalyticsOptions = new GoogleAnalyticsOptions { CookiePrefix = GoogleAnalyticsDefaultCookieName };

        _cookieOptions = new Mock<IOptions<CookieOptions>>();
        _cookieOptions.Setup(ap => ap.Value).Returns(eprCookieOptions);

        _googleAnalyticsOptions = new Mock<IOptions<GoogleAnalyticsOptions>>();
        _googleAnalyticsOptions.Setup(ap => ap.Value).Returns(googleAnalyticsOptions);

        _systemUnderTest = new CookieService(
            _loggerMock.Object,
            _cookieOptions.Object,
            _googleAnalyticsOptions.Object);
    }
}