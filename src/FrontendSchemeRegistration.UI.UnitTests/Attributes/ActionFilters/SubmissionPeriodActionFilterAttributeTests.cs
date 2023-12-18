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
using Moq;
using UI.Attributes.ActionFilters;
using UI.Sessions;

[TestFixture]
public class SubmissionPeriodActionFilterAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private SubmissionPeriodActionFilterAttribute _systemUnderTest;
    private ActionExecutingContext _actionExecutingContext;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(ISessionManager<FrontendSchemeRegistrationSession>))).Returns(_sessionMock.Object);
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
        _systemUnderTest = new SubmissionPeriodActionFilterAttribute(PagePaths.FileUpload);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSubmissionPeriodIsPresent()
    {
        // Arrange
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new()
            {
                SubmissionPeriod = "some submission period"
            }
        });

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToPagePath_WhenSubmissionPeriodIsNotPresent()
    {
        // Arrange
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.FileUpload}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToPagePath_WhenSessionIsNull()
    {
        // Arrange

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.FileUpload}");
        _delegateMock.Verify(next => next(), Times.Never);
    }
}