namespace FrontendSchemeRegistration.UI.UnitTests.Middleware.JourneyAccessChecker;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Attributes;
using FrontendSchemeRegistration.UI.Middleware;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;

[TestFixture]
public class JourneyAccessCheckerMiddlewareDelegatedPersonTests
{
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<HttpRequest> _httpRequestMock = new();
    private readonly Mock<IFeatureCollection> _featureCollectionMock = new();
    private readonly Mock<IEndpointFeature> _endpointFeatureMock = new();
    private readonly Guid _enrolmentId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b");
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<HttpResponse> _httpResponseMock;
    private IOptions<GlobalVariables> _globalVariablesOptions;
    private JourneyAccessCheckerMiddleware _middleware;

    [SetUp]
    public void Setup()
    {
        _httpResponseMock = new Mock<HttpResponse>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

        _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
        _httpContextMock.Setup(x => x.Response).Returns(_httpResponseMock.Object);
        _httpContextMock.Setup(x => x.Features).Returns(_featureCollectionMock.Object);

        _featureCollectionMock.Setup(x => x.Get<IEndpointFeature>()).Returns(_endpointFeatureMock.Object);

        _middleware = new JourneyAccessCheckerMiddleware(_ => Task.CompletedTask);

        _globalVariablesOptions = Options.Create(new GlobalVariables { BasePath = "/report-data" });
    }

    [Test]
    [TestCase(
        PagePaths.ConfirmPermissionSubmitData,
        $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"/{PagePaths.HomePageSelfManaged}",
        $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b")]
    [TestCase(
        PagePaths.ConfirmPermissionSubmitData,
        $"{PagePaths.TelephoneNumber}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"/{PagePaths.HomePageSelfManaged}",
        $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"{PagePaths.TelephoneNumber}/147f59f0-3d4e-4557-91d2-db033dffa60b")]
    public async Task GivenAccessRequiredPage_WhichIsNotPartOfTheVisitedURLs_WhenInvokeCalled_ThenRedirectedToExpectedPage(
        string pageUrl, string expectedPage, params string[] visitedUrls)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new() { Journey = visitedUrls.ToList() } };
        var expectedURL = $"/report-data{expectedPage}";

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl, JourneyName.NominatedDelegatedPerson));

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var routeData = new RouteData();
        routeData.Values.Add("id", _enrolmentId);
        _httpRequestMock.Setup(x => x.RouteValues).Returns(routeData.Values);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(expectedURL), Times.Once);
    }

    [Test]
    [TestCase(
        PagePaths.TelephoneNumber, $"/{PagePaths.HomePageSelfManaged}",
        $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"{PagePaths.TelephoneNumber}/147f59f0-3d4e-4557-91d2-db033dffa60b")]
    [TestCase(
        PagePaths.ConfirmPermissionSubmitData, $"/{PagePaths.HomePageSelfManaged}",
        $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"{PagePaths.TelephoneNumber}/147f59f0-3d4e-4557-91d2-db033dffa60b",
        $"{PagePaths.ConfirmPermissionSubmitData}/147f59f0-3d4e-4557-91d2-db033dffa60b")]
    public async Task GivenAccessRequiredPage_WhichIsPartOfTheVisitedURLs_WhenInvokeCalled_ThenNoRedirectionHappened(
        string pageUrl, params string[] visitedUrls)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new() { Journey = visitedUrls.ToList() } };

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl, JourneyName.NominatedDelegatedPerson));

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var routeData = new RouteData();
        routeData.Values.Add("id", _enrolmentId);
        _httpRequestMock.Setup(x => x.RouteValues).Returns(routeData.Values);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(It.IsAny<string>()), Times.Never);
    }

    [Test]
    [TestCase(PagePaths.InviteChangePermissions)]
    [TestCase(PagePaths.TelephoneNumber)]
    [TestCase(PagePaths.ConfirmPermissionSubmitData)]
    public async Task GivenAccessRequiredPage_WithoutStoredSession_WhenInvokeCalled_ThenRedirectedToFirstPage(string pageUrl)
    {
        // Arrange
        var expectedURL = $"/report-data/{PagePaths.HomePageSelfManaged}";

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl, JourneyName.NominatedDelegatedPerson));

        var routeData = new RouteData();
        routeData.Values.Add("id", _enrolmentId);
        _httpRequestMock.Setup(x => x.RouteValues).Returns(routeData.Values);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(expectedURL), Times.Once);
    }

    [Test]
    [TestCase(PagePaths.InviteChangePermissions)]
    [TestCase(PagePaths.TelephoneNumber)]
    [TestCase(PagePaths.ConfirmPermissionSubmitData)]
    public async Task GivenAccessRequiredPage_WithoutEnrolmentId_WhenInvokeCalled_ThenRedirectedToFirstPage(string pageUrl)
    {
        // Arrange
        var expectedURL = $"/report-data/{PagePaths.HomePageSelfManaged}";

        string[] visitedUrls =
            {
                $"/{PagePaths.HomePageSelfManaged}",
                $"{PagePaths.InviteChangePermissions}/147f59f0-3d4e-4557-91d2-db033dffa60b",
                $"{PagePaths.TelephoneNumber}/147f59f0-3d4e-4557-91d2-db033dffa60b",
                $"{PagePaths.ConfirmPermissionSubmitData}/147f59f0-3d4e-4557-91d2-db033dffa60b"
            };

        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new() { Journey = visitedUrls.ToList() } };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        SetupEndpointMock(new JourneyAccessAttribute(pageUrl, JourneyName.NominatedDelegatedPerson));

        var routeData = new RouteData();
        _httpRequestMock.Setup(x => x.RouteValues).Returns(routeData.Values);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(expectedURL), Times.Once);
    }

    private void SetupEndpointMock(params object[] attributes)
    {
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(attributes), null);

        _endpointFeatureMock.Setup(x => x.Endpoint).Returns(endpoint);
    }
}
