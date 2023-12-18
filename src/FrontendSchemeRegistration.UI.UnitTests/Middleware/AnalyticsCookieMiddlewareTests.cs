namespace FrontendSchemeRegistration.UI.UnitTests.Middleware;

using Application.Options;
using Application.Services.Interfaces;
using AutoFixture;
using Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using UI.Middleware;

[TestFixture]
public class AnalyticsCookieMiddlewareTests
{
    private Mock<RequestDelegate> _mockRequestDelegate;
    private Mock<ICookieService> _mockCookieService;
    private Mock<IOptions<GoogleAnalyticsOptions>> _mockAnalyticsOptions;
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _mockRequestDelegate = new Mock<RequestDelegate>();
        _mockCookieService = new Mock<ICookieService>();
        _mockAnalyticsOptions = new Mock<IOptions<GoogleAnalyticsOptions>>();
        _fixture = new Fixture();
    }

    [Test]
    public async Task InvokeAsync_ShouldSetHttpContextItems()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var middleware = new AnalyticsCookieMiddleware(_mockRequestDelegate.Object);
        var hasUserAcceptedCookies = _fixture.Create<bool>();
        var tagManagerContainerId = _fixture.Create<string>();

        _mockCookieService.Setup(s => s.HasUserAcceptedCookies(It.IsAny<IRequestCookieCollection>())).Returns(hasUserAcceptedCookies);
        _mockAnalyticsOptions.Setup(s => s.Value)
            .Returns(new GoogleAnalyticsOptions { TagManagerContainerId = tagManagerContainerId });

        // Act
        await middleware.InvokeAsync(httpContext, _mockCookieService.Object, _mockAnalyticsOptions.Object);

        // Assert
        httpContext.Items[ContextKeys.UseGoogleAnalyticsCookieKey].Should().Be(hasUserAcceptedCookies);
        httpContext.Items[ContextKeys.TagManagerContainerIdKey].Should().Be(tagManagerContainerId);
    }
}