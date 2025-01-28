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

namespace FrontendSchemeRegistration.UI.UnitTests.Middleware.JourneyAccessChecker;

using Constants;

public class SchemeMembershipTests
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
    public async Task WhenPageFromJourneyIsAccessed_ThenNoRedirectionHappens()
    {
        // Arrange
        SetSchemeMembershipJourney(PagePaths.SchemeMembers, PagePaths.MemberDetails);
        SetRequestedSchemeMembershipPage(PagePaths.SchemeMembers);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        _httpResponseMock.Verify(x => x.Redirect(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task WhenPageFromOutsideOfJourneyIsAccessed_ThenNoRedirectToLastValidJourneyPath()
    {
        // Arrange
        SetSchemeMembershipJourney(PagePaths.SchemeMembers, PagePaths.MemberDetails);
        SetRequestedSchemeMembershipPage(PagePaths.ReasonsForRemoval);

        // Act
        await _middleware.Invoke(_httpContextMock.Object, _sessionManagerMock.Object, _globalVariablesOptions);

        // Assert
        var redirectUrl = $"{_globalVariablesOptions.Value.BasePath}{PagePaths.MemberDetails}";

        _httpResponseMock.Verify(x => x.Redirect(redirectUrl), Times.Once);
    }

    private void SetSchemeMembershipJourney(params string[] visitedPages)
    {
        var session = new FrontendSchemeRegistrationSession();

        session.SchemeMembershipSession.Journey.AddRange(visitedPages);

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
    }

    private void SetRequestedSchemeMembershipPage(string requestedPage)
    {
        var attribute = new JourneyAccessAttribute(requestedPage, JourneyName.SchemeMembership);

        var endpoint = new Endpoint(null, new EndpointMetadataCollection(attribute), null);

        _endpointFeatureMock.Setup(x => x.Endpoint).Returns(endpoint);

        _httpContextMock.Setup(httpContext => httpContext.Request.Path).Returns(requestedPage);
    }
}