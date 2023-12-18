namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using System.Security.Claims;
using System.Text.Json;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Constants;
using EPR.Common.Authorization.Models;
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
public class ComplianceSchemeIdActionFilterAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private ComplianceSchemeIdActionFilterAttribute _systemUnderTest;
    private ActionExecutingContext _actionExecutingContext;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(ISessionManager<FrontendSchemeRegistrationSession>))).Returns(_sessionMock.Object);
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _actionExecutingContext = new ActionExecutingContext(
            new ActionContext(
                new DefaultHttpContext
                {
                    RequestServices = _serviceProviderMock.Object,
                    Session = new Mock<ISession>().Object,
                    User = _claimsPrincipalMock.Object
                },
                Mock.Of<RouteData>(),
                Mock.Of<ActionDescriptor>(),
                Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Mock.Of<Controller>());
        _systemUnderTest = new ComplianceSchemeIdActionFilterAttribute();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenOrganisationIsComplianceSchemeAndSelectedComplianceSchemeIdIsPresent()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Id = Guid.NewGuid()
                }
            }
        });

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToPagePath_WhenOrganisationIsComplianceSchemeAndSelectedComplianceSchemeIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.ComplianceSchemeLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToPagePath_WhenOrganisationIsComplianceSchemeAndSessionIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.ComplianceSchemeLanding}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenOrganisationIsProducerAndSelectedComplianceSchemeIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenOrganisationIsProducerAndSessionIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }

    private static List<Claim> CreateUserDataClaim(string organisationRole)
    {
        var userData = new UserData
        {
            Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganisationRole = organisationRole
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}