namespace FrontendSchemeRegistration.UI.UnitTests.Middleware;

using Application.DTOs.UserAccount;
using Application.Options;
using Application.Services.Interfaces;
using Controllers;
using EPR.Common.Authorization.Services;
using EPR.Common.Authorization.Services.Interfaces;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using System.Security.Claims;
using UI.Middleware;

[TestFixture]
public class UserDataCheckerMiddlewareTests : FrontendSchemeRegistrationTestBase
{
    private const string BaseUrl = "some-base-path";
    private const string OrganisationIdsExtensionClaimName = "OrgIds";
    private readonly FrontEndAccountCreationOptions _frontEndAccountCreationOptions = new() { BaseUrl = BaseUrl };
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private Mock<HttpContext> _httpContextMock;
    private Mock<RequestDelegate> _requestDelegateMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private Mock<IGraphService> _graphServiceMock;
    private Mock<ILogger<UserDataCheckerMiddleware>> _loggerMock;
    private Mock<ControllerActionDescriptor> _controllerActionDescriptor;
    private UserDataCheckerMiddleware _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _requestDelegateMock = new Mock<RequestDelegate>();
        _httpContextMock = new Mock<HttpContext>();
        _loggerMock = new Mock<ILogger<UserDataCheckerMiddleware>>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _graphServiceMock = new Mock<IGraphService>();
        _controllerActionDescriptor = new Mock<ControllerActionDescriptor>();

        var metadata = new List<object> { _controllerActionDescriptor.Object };
        _httpContextMock.Setup(x => x.Features.Get<IEndpointFeature>().Endpoint).Returns(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "Privacy"));

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(false);

        _systemUnderTest = new UserDataCheckerMiddleware(
            Options.Create(_frontEndAccountCreationOptions),
            _userAccountServiceMock.Object,
            _featureManagerMock.Object,
            _graphServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Middleware_DoesNotCallUserAccountService_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _claimsPrincipalMock.Setup(x => x.Identity.IsAuthenticated).Returns(false);
        _httpContextMock.Setup(x => x.User).Returns(_claimsPrincipalMock.Object);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Never);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Once);
    }

    [Test]
    public async Task Middleware_DoesNotCallUserAccountService_WhenUserDataAlreadyExistsInUserClaims()
    {
        // Arrange
        _claimsPrincipalMock.Setup(x => x.Identity.IsAuthenticated).Returns(true);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(new List<Claim> { new(ClaimTypes.UserData, "{}") });
        _httpContextMock.Setup(x => x.User).Returns(_claimsPrincipalMock.Object);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Never);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Once);
    }

    [Test]
    public async Task Middleware_CallsUserAccountServiceAndSignsIn_WhenUserDataDoesNotExistInUserClaims()
    {
        // Arrange
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Once);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Once);
    }

    [Test]
    public async Task Middleware_RedirectToFrontendAccountCreation_WhenUserAccountServiceDoesNotReturnDataForUser()
    {
        // Arrange
        var httpResponseMock = new Mock<HttpResponse>();
        _claimsPrincipalMock.Setup(x => x.Identity.IsAuthenticated).Returns(true);
        _httpContextMock.Setup(x => x.User).Returns(_claimsPrincipalMock.Object);
        _httpContextMock.Setup(x => x.Response).Returns(httpResponseMock.Object);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Once);
        httpResponseMock.Verify(x => x.Redirect(BaseUrl), Times.Once);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Never);
    }

    [Test]
    public async Task Middleware_CallsUserAccountServiceAndSignsIn_WhenUserDataDoesNotExistInUserClaims_WithNullEndpoint()
    {
        // Arrange
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.Features.Get<IEndpointFeature>().Endpoint).Returns((Endpoint)null);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Once);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Once);
    }

    [Test]
    public async Task Middleware_LogsOrgIdsClaim_WhenClaimIsPresentAtSignIn()
    {
        // Arrange
        const string orgIds = "12345,67890";
        const string expectedLog = $"Found claim {CustomClaimTypes.OrganisationIds} with value {orgIds}";

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new(CustomClaimTypes.OrganisationIds, orgIds) }, "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _userAccountServiceMock.Verify(x => x.GetUserAccount(), Times.Once);
        _requestDelegateMock.Verify(x => x(_httpContextMock.Object), Times.Once);

        _loggerMock.VerifyLog(logger => logger.LogInformation(expectedLog), Times.Once);
    }

    [Test]
    public async Task Middleware_CallsGraphService_WhenOrgIdsClaimIsEmpty()
    {
        // Arrange
        const string orgIds = "123456";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), OrganisationIdsExtensionClaimName, orgIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Middleware_DoesNotCallGraphService_WhenOrgIdsClaimIsEmpty_And_GraphApiFeature_IsNotEnabled()
    {
        // Arrange
        const string orgIds = "123456";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Middleware_CallsGraphService_WithMultipleOrganisationNumbers()
    {
        // Arrange
        const string orgIds = "123456,078910";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        var user = GetUserAccount();
        AddSecondOrganisationToUser(user);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(user);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), OrganisationIdsExtensionClaimName, orgIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Middleware_DoesNotCallGraphService_WhenOrgIdsClaimMatches()
    {
        // Arrange
        var orgIds = "123456";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new(CustomClaimTypes.OrganisationIds, orgIds) }, "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Middleware_DoesNotThrowException_WhenGraphServiceIsNulllGraphService()
    {
        // Arrange
        const string orgIds = "123456";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new(CustomClaimTypes.OrganisationIds, orgIds) }, "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(GetUserAccount());

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        var graphService = new NullGraphService();

        _systemUnderTest = new UserDataCheckerMiddleware(
            Options.Create(_frontEndAccountCreationOptions),
            _userAccountServiceMock.Object,
            _featureManagerMock.Object,
            graphService,
            _loggerMock.Object);

        // Act
        var act = async () => await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        await act.Should().NotThrowAsync<Exception>();
    }

    [Test]
    public async Task Middleware_ClearsOrgIds_And_CallsGraphService_WhenUserHasNoServiceRole()
    {
        // Arrange
        const string orgIds = "123456,078910";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new(CustomClaimTypes.OrganisationIds, orgIds) }, "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        var user = GetUserAccount();
        user.User.ServiceRole = null;
        user.User.ServiceRoleId = 0;
        AddSecondOrganisationToUser(user);
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(user);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), OrganisationIdsExtensionClaimName, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task Middleware_DoesNotCallGraphService_WhenOrgIdsClaimIsEmpty_AndUserHasNoServiceRole()
    {
        // Arrange
        const string orgIds = "123456";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "authenticationType"));
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(Mock.Of<IAuthenticationService>());
        _httpContextMock.Setup(x => x.User).Returns(claims);
        _httpContextMock.Setup(x => x.RequestServices).Returns(serviceProviderMock.Object);

        var user = GetUserAccount();
        user.User.ServiceRole = null;
        user.User.ServiceRoleId = 0;
        _userAccountServiceMock.Setup(x => x.GetUserAccount()).ReturnsAsync(user);

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.InvokeAsync(_httpContextMock.Object, _requestDelegateMock.Object);

        // Assert
        _graphServiceMock.Verify(x => x.PatchUserProperty(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UserAccountDto GetUserAccount()
    {
        return new UserAccountDto
        {
            User = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Joe",
                LastName = "Test",
                Email = "JoeTest@something.com",
                RoleInOrganisation = "Test Role",
                EnrolmentStatus = "Enrolled",
                ServiceRole = "Test service role",
                Service = "Test service",
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrganisationName = "TestCo",
                        OrganisationNumber = "123456",
                        OrganisationRole = "Producer",
                        OrganisationType = "test type",
                    },
                },
            },
        };
    }

    private static void AddSecondOrganisationToUser(UserAccountDto user)
    {
        user.User.Organisations.Add(new()
        {
            Id = Guid.NewGuid(),
            OrganisationName = "SecondTestCo",
            OrganisationNumber = "078910",
            OrganisationRole = "Member",
            OrganisationType = "test type 2",
        });
    }
}