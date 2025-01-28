using System.Security.Claims;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

[TestFixture]
public class ComplianceSchemeIdHttpContextFilterAttributeTests
{
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private ComplianceSchemeIdHttpContextFilterAttribute _filter;
    [SetUp]
    public void SetUp()
    {
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _filter = new ComplianceSchemeIdHttpContextFilterAttribute(_sessionManagerMock.Object);
    }

    [Test]
    public async Task OnActionExecutionAsync_SetsComplianceSchemeId_WhenOrganisationRoleIsComplianceScheme()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        
        // Mock the session
        var sessionMock = new Mock<ISession>();
        httpContext.Session = sessionMock.Object;
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, "{\"Organisations\":[{\"OrganisationRole\":\"Compliance Scheme\"}]}") // Add UserData claim
        }));
        
        httpContext.User = claimsPrincipal;
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            }
        };
        
        _sessionManagerMock
            .Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);
        
        var routeData = new RouteData();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = new ActionDescriptor()
        };
        
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
        
        var next = new Mock<ActionExecutionDelegate>();
        
        // Act
        await _filter.OnActionExecutionAsync(context, next.Object);
        
        // Assert
        Assert.That(httpContext.Items[ComplianceScheme.ComplianceSchemeId], Is.EqualTo(session.RegistrationSession.SelectedComplianceScheme.Id));
        next.Verify(n => n(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_DoesNotSetComplianceSchemeId_WhenOrganisationRoleIsNotComplianceScheme()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // Mock the session
        var sessionMock = new Mock<ISession>();
        httpContext.Session = sessionMock.Object; // Assign the mocked session
        // Add the required UserData claim with a role that is not "Compliance Scheme"
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, "{\"Organisations\":[{\"OrganisationRole\":\"OtherRole\"}]}") // Add UserData claim
        }));
        httpContext.User = claimsPrincipal;
        var routeData = new RouteData();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = new ActionDescriptor()
        };
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
        var next = new Mock<ActionExecutionDelegate>();
        // Act
        await _filter.OnActionExecutionAsync(context, next.Object);
        // Assert
        Assert.That(httpContext.Items.ContainsKey(ComplianceScheme.ComplianceSchemeId), Is.False);
        next.Verify(n => n(), Times.Once);
    }


    [Test]
    public void OnActionExecutionAsync_ThrowsInvalidOperationException_WhenUserDataClaimIsMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // No UserData claim
        var routeData = new RouteData();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = new ActionDescriptor()
        };
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _filter.OnActionExecutionAsync(context, Mock.Of<ActionExecutionDelegate>()));
    }
    [Test]
    public async Task OnActionExecutionAsync_DoesNotSetComplianceSchemeId_WhenHttpContextSessionIsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Session = null; // Null session
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, "{\"Organisations\":[{\"OrganisationRole\":\"Compliance Scheme\"}]}") // Valid UserData claim
        }));
        httpContext.User = claimsPrincipal;
        var routeData = new RouteData();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = new ActionDescriptor()
        };
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
        var next = new Mock<ActionExecutionDelegate>();
        // Act
        await _filter.OnActionExecutionAsync(context, next.Object);
        // Assert
        Assert.That(httpContext.Items.ContainsKey(ComplianceScheme.ComplianceSchemeId), Is.False);
        next.Verify(n => n(), Times.Once);
    }
}