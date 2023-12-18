namespace FrontendSchemeRegistration.UI.UnitTests.Middleware;

using System.Security.Claims;
using Application.DTOs.UserAccount;
using Application.Options;
using Application.Services.Interfaces;
using Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UI.Middleware;

[TestFixture]
public class UserDataCheckerMiddlewareTests : FrontendSchemeRegistrationTestBase
{
    private const string BaseUrl = "some-base-path";
    private readonly FrontEndAccountCreationOptions _frontEndAccountCreationOptions = new() { BaseUrl = BaseUrl };
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private Mock<HttpContext> _httpContextMock;
    private Mock<RequestDelegate> _requestDelegateMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
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
        _controllerActionDescriptor = new Mock<ControllerActionDescriptor>();

        var metadata = new List<object> { _controllerActionDescriptor.Object };

        _httpContextMock.Setup(x => x.Features.Get<IEndpointFeature>().Endpoint).Returns(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "Privacy"));
        _systemUnderTest = new UserDataCheckerMiddleware(Options.Create(_frontEndAccountCreationOptions), _userAccountServiceMock.Object, _loggerMock.Object);
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
                        OrganisationRole = "Producer",
                        OrganisationType = "test type",
                    },
                },
            },
        };
    }
}