namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class RegistrationApplicationSessionLoggingScopeActionFilterAttributeTests
{
    [Test]
    public async Task OnActionExecutionAsync_CallsNext_AndLogsNullMessage_WhenRegistrationApplicationSessionIsNull()
    {
        // Arrange
        var logger = CreateLoggerMock(out var loggerMock);

        var regAppSessionManager = new Mock<ISessionManager<RegistrationApplicationSession>>();
        regAppSessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((RegistrationApplicationSession?)null);

        var frontEndSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        frontEndSessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        var httpContext = BuildHttpContext(logger, regAppSessionManager.Object, frontEndSessionManager.Object);
        var executingContext = BuildActionExecutingContext(httpContext);

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), new object()));
        };

        var sut = new RegistrationApplicationSessionLoggingScopeActionFilterAttribute();

        // Act
        await sut.OnActionExecutionAsync(executingContext, next);

        // Assert
        nextCalled.Should().BeTrue();
        loggerMock.VerifyLogContains(LogLevel.Information, "OnActionEntry: RegistrationSession is null", Times.Once());
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_AndLogsFoundMessage_WhenRegistrationApplicationSessionIsPresent()
    {
        // Arrange
        var logger = CreateLoggerMock(out var loggerMock);

        var regAppSessionManager = new Mock<ISessionManager<RegistrationApplicationSession>>();
        regAppSessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "APP-REF-123",
                SubmissionPeriod = "January to December 2026",
            });

        var frontEndSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        frontEndSessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "APP-REF-456", IsResubmission = true }
            });

        var httpContext = BuildHttpContext(logger, regAppSessionManager.Object, frontEndSessionManager.Object);
        var executingContext = BuildActionExecutingContext(httpContext);

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), new object()));
        };

        var sut = new RegistrationApplicationSessionLoggingScopeActionFilterAttribute();

        // Act
        await sut.OnActionExecutionAsync(executingContext, next);

        // Assert
        nextCalled.Should().BeTrue();
        loggerMock.VerifyLogContains(LogLevel.Information, "OnActionEntry: RegistrationSession found", Times.Once());
    }

    private static DefaultHttpContext BuildHttpContext(
        ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute> logger,
        ISessionManager<RegistrationApplicationSession> regAppSessionManager,
        ISessionManager<FrontendSchemeRegistrationSession> frontEndSessionManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(logger);
        services.AddSingleton(regAppSessionManager);
        services.AddSingleton(frontEndSessionManager);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var sessionFeature = new Mock<ISessionFeature>();
        sessionFeature.SetupGet(x => x.Session).Returns(Mock.Of<ISession>());
        httpContext.Features.Set<ISessionFeature>(sessionFeature.Object);
        return httpContext;
    }

    private static ActionExecutingContext BuildActionExecutingContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { DisplayName = "RegistrationApplicationController.SomeAction (Test)" },
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute> CreateLoggerMock(
        out Mock<ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute>> loggerMock)
    {
        loggerMock = new Mock<ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute>>();

        // BeginScope is used with both formatted scope and dictionary scopes.
        loggerMock
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        return loggerMock.Object;
    }

}

internal static class LoggerMoqExtensions
{
    public static void VerifyLogContains<TLogger>(
        this Mock<ILogger<TLogger>> loggerMock,
        LogLevel level,
        string expectedSubstring,
        Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() != null && state.ToString()!.Contains(expectedSubstring)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}

