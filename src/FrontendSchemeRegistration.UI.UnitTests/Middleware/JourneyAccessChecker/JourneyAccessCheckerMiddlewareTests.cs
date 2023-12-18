namespace FrontendSchemeRegistration.UI.UnitTests.Middleware.JourneyAccessChecker;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Controllers.Attributes;
using FrontendSchemeRegistration.UI.Middleware;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Moq;

[TestFixture]
public class JourneyAccessCheckerMiddlewareTests
{
    private Mock<HttpContext> _httpContextMock;
    private Mock<HttpResponse> _httpResponseMock;
    private Mock<IFeatureCollection> _featureCollectionMock;
    private Mock<IEndpointFeature> _endpointFeatureMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private IOptions<GlobalVariables> _globalVariablesOptions;

    private JourneyAccessCheckerMiddleware _middleware;

    [SetUp]
    public void Setup()
    {
        _httpContextMock = new Mock<HttpContext>();
        _httpResponseMock = new Mock<HttpResponse>();
        _featureCollectionMock = new Mock<IFeatureCollection>();
        _endpointFeatureMock = new Mock<IEndpointFeature>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

        _httpContextMock.Setup(x => x.Response).Returns(_httpResponseMock.Object);
        _httpContextMock.Setup(x => x.Features).Returns(_featureCollectionMock.Object);
        _featureCollectionMock.Setup(x => x.Get<IEndpointFeature>()).Returns(_endpointFeatureMock.Object);

        _middleware = new JourneyAccessCheckerMiddleware(_ => Task.CompletedTask);

        _globalVariablesOptions = Options.Create(
            new GlobalVariables
            {
                BasePath = "/report-data"
            });
    }

    [Test]
    [TestCase(PagePaths.UsingAComplianceScheme, PagePaths.HomePageSelfManaged)]
    [TestCase(PagePaths.UsingAComplianceScheme, PagePaths.HomePageSelfManaged, PagePaths.HomePageSelfManaged)]
    public async Task GivenAccessRequiredPage_WhichIsNotPartOfTheVisitedURLs_WhenInvokeCalled_ThenRedirectedToExpectedPage(
        string pageUrl, string expectedPage, params string[] visitedUrls)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { RegistrationSession = new() { Journey = visitedUrls.ToList() } };
        var expectedURL = expectedPage;

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl));

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(expectedURL), Times.Once);
    }

    [Test]
    [TestCase(PagePaths.UsingAComplianceScheme, PagePaths.HomePageSelfManaged, PagePaths.UsingAComplianceScheme)]
    public async Task GivenAccessRequiredPage_WhichIsPartOfTheVisitedURLs_WhenInvokeCalled_ThenNoRedirectionHappened(
        string pageUrl, params string[] visitedUrls)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { RegistrationSession = new() { Journey = visitedUrls.ToList() } };

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl));

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(It.IsAny<string>()), Times.Never);
    }

    [Test]
    [TestCase(PagePaths.UsingAComplianceScheme)]
    public async Task GivenAccessRequiredPage_WithoutStoredSession_WhenInvokeCalled_ThenRedirectedToFirstPage(string pageUrl)
    {
        // Arrange
        const string firstPageUrl = PagePaths.HomePageSelfManaged;
        SetupEndpointMock(new JourneyAccessAttribute(pageUrl));

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(firstPageUrl), Times.Once);
    }

    [Test]
    [TestCase(PagePaths.HomePageSelfManaged)]
    public async Task GivenNoAccessRequiredPage_WhenInvokeCalled_ThenNoRedirectionHappened(string pageUrl)
    {
        // Arrange
        SetupEndpointMock();

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(It.IsAny<string>()), Times.Never);
    }

    private void SetupEndpointMock(params object[] attributes)
    {
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(attributes), null);

        _endpointFeatureMock.Setup(x => x.Endpoint).Returns(endpoint);
    }
}
