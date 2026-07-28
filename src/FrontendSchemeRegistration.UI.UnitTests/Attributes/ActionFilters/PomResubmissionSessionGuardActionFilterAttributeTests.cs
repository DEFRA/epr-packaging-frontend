namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using Application.Constants;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using Moq;
using UI.Attributes.ActionFilters;
using UI.Sessions;

[TestFixture]
public class PomResubmissionSessionGuardActionFilterAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private PomResubmissionSessionGuardActionFilterAttribute _systemUnderTest;
    private ActionExecutingContext _actionExecutingContext;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ISessionManager<FrontendSchemeRegistrationSession>)))
            .Returns(_sessionMock.Object);
        _actionExecutingContext = new ActionExecutingContext(
            new ActionContext(
                new DefaultHttpContext
                {
                    RequestServices = _serviceProviderMock.Object,
                    Session = new Mock<ISession>().Object
                },
                Mock.Of<RouteData>(),
                Mock.Of<ActionDescriptor>(),
                Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Mock.Of<Controller>());
        _systemUnderTest = new PomResubmissionSessionGuardActionFilterAttribute();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSubmissionIdAndSubmissionPeriodArePresent()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = "January to December 2024",
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    SubmissionId = Guid.NewGuid()
                }
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeNull();
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToFileUploadSubLanding_WhenSessionIsNull()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession?)null);

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>()
            .Subject.Url.Should().Be($"~{PagePaths.FileUploadSubLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToFileUploadSubLanding_WhenSubmissionIdIsNull()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = "January to December 2024"
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>()
            .Subject.Url.Should().Be($"~{PagePaths.FileUploadSubLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToFileUploadSubLanding_WhenSubmissionPeriodIsMissing()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = null,
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    SubmissionId = Guid.NewGuid()
                }
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>()
            .Subject.Url.Should().Be($"~{PagePaths.FileUploadSubLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_SetsCacheControlNoStore_WhenSubmissionIdAndSubmissionPeriodArePresent()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = "January to December 2024",
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    SubmissionId = Guid.NewGuid()
                }
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.HttpContext.Response.Headers[HeaderNames.CacheControl].ToString().Should().Be("no-store");
    }

    [Test]
    public async Task OnActionExecutionAsync_SetsCacheControlNoStore_WhenSessionIsWiped()
    {
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession());

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.HttpContext.Response.Headers[HeaderNames.CacheControl].ToString().Should().Be("no-store");
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenRequireSubmissionIdIsFalseAndSubmissionIdIsMissing()
    {
        _systemUnderTest = new PomResubmissionSessionGuardActionFilterAttribute { RequireSubmissionId = false };
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = "January to December 2024"
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeNull();
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToFileUploadSubLanding_WhenRequireSubmissionIdIsFalseButSubmissionPeriodIsMissing()
    {
        _systemUnderTest = new PomResubmissionSessionGuardActionFilterAttribute { RequireSubmissionId = false };
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = null,
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    SubmissionId = Guid.NewGuid()
                }
            }
        });

        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>()
            .Subject.Url.Should().Be($"~{PagePaths.FileUploadSubLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }
}
