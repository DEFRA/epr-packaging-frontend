using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    [TestFixture]
    public class NominatedApprovedPersonControllerTests
    {
        private NominatedApprovedPersonController _systemUnderTest;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
        private Mock<IOptions<GlobalVariables>> _globalVariablesMock;
        private Mock<INotificationService> _notificationServiceMock;
        private Mock<IRoleManagementService> _roleManagementServiceMock;
        private Mock<ClaimsPrincipal> _claimsPrincipalMock;
        private Guid _organisationId = Guid.NewGuid();

        [SetUp]
        public void SetUp()
        {
            _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _globalVariablesMock = new Mock<IOptions<GlobalVariables>>();
            _notificationServiceMock = new Mock<INotificationService>();
            _roleManagementServiceMock = new Mock<IRoleManagementService>();
            _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            _globalVariablesMock.Setup(g => g.Value).Returns(new GlobalVariables()
            {
                BasePath = "/",
            });
            _systemUnderTest = new NominatedApprovedPersonController(_sessionManagerMock.Object, _globalVariablesMock.Object,
                    _notificationServiceMock.Object, _roleManagementServiceMock.Object);

            _systemUnderTest.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    Session = new Mock<ISession>().Object,
                    User = _claimsPrincipalMock.Object,
                }
            };
        }

        [Test]
        public async Task InviteChangePermissions_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange

            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.InviteChangePermissions(Guid.NewGuid());

            // Assert

            result.Should().BeOfType<RedirectToActionResult>()
            .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task InviteChangePermissions_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var claims = CreateUserDataClaim(Guid.NewGuid(), string.Empty, string.Empty, OrganisationRoles.Producer);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            // Act
            var result = await _systemUnderTest.InviteChangePermissions(id);
            // Assert

            var model = result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<InviteChangePermissionsViewModel>().Subject.As<InviteChangePermissionsViewModel>();
            model.Id.Should().Be(id);
            model.OrganisationName.Should().Be("Some org");
            model.IsInCompaniesHouse.Should().BeTrue();
         }

        [Test]
        public async Task RoleInOrganisation_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.RoleInOrganisation(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task RoleInOrganisation_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            frontendSchemeRegistrationSession.NominatedApprovedPersonSession.RoleInOrganisation = "CompanySecretary";
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);

            // Act
            var result = await _systemUnderTest.RoleInOrganisation(id);

            // Assert
            var model = result.Should().BeOfType<ViewResult>()
                              .Which.Model.Should().BeOfType<RoleInOrganisationViewModel>()
                              .Subject.As<RoleInOrganisationViewModel>();
            model.Id.Should().Be(id);
            model.RoleInOrganisation.Should().Be(RoleInOrganisation.CompanySecretary);
        }

        [Test]
        public async Task RoleInOrganisation_Post_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
            var model = new RoleInOrganisationViewModel();

            // Act
            var result = await _systemUnderTest.RoleInOrganisation(model, Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task RoleInOrganisation_Post_ReturnsViewResult_WithInvalidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new RoleInOrganisationViewModel();
            _systemUnderTest.ModelState.AddModelError("RoleInOrganisation", "The RoleInOrganisation field is required.");

            // Act
            var result = await _systemUnderTest.RoleInOrganisation(model, id);

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().Be("RoleInOrganisation");
        }

        [Test]
        public async Task RoleInOrganisation_Post_ReturnsRedirectToAction_WithValidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new RoleInOrganisationViewModel { RoleInOrganisation = RoleInOrganisation.CompanySecretary };

            // Act
            var result = await _systemUnderTest.RoleInOrganisation(model, id);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("TelephoneNumber");
        }

        [Test]
        public async Task ManualRoleInOrganisation_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.ManualRoleInOrganisation(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task ManualRoleInOrganisation_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession { RoleInOrganisation = "Director" } };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);

            // Act
            var result = await _systemUnderTest.ManualRoleInOrganisation(id);

            // Assert
            var model = result.Should().BeOfType<ViewResult>()
                              .Which.Model.Should().BeOfType<ManualRoleInOrganisationViewModel>()
                              .Subject.As<ManualRoleInOrganisationViewModel>();
            model.Id.Should().Be(id);
            model.RoleInOrganisation.Should().Be("Director");
        }

        [Test]
        public async Task ManualRoleInOrganisation_Post_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
            var model = new ManualRoleInOrganisationViewModel();

            // Act
            var result = await _systemUnderTest.ManualRoleInOrganisation(model, Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task ManualRoleInOrganisation_Post_ReturnsViewResult_WithInvalidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new ManualRoleInOrganisationViewModel();
            _systemUnderTest.ModelState.AddModelError("RoleInOrganisation", "The RoleInOrganisation field is required.");

            // Act
            var result = await _systemUnderTest.ManualRoleInOrganisation(model, id);

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().Be("ManualRoleInOrganisation");
        }

        [Test]
        public async Task ManualRoleInOrganisation_Post_ReturnsRedirectToAction_WithValidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new ManualRoleInOrganisationViewModel { RoleInOrganisation = "SomeRole" };

            // Act
            var result = await _systemUnderTest.ManualRoleInOrganisation(model, id);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("TelephoneNumber");
        }

        [Test]
        public async Task TelephoneNumber_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.TelephoneNumber(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task TelephoneNumber_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession { TelephoneNumber = "123456789" } };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var userData = new UserData { Email = "test@example.com" };
            var claims = new List<Claim> { new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData)) };
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            // Act
            var result = await _systemUnderTest.TelephoneNumber(id);

            // Assert
            var model = result.Should().BeOfType<ViewResult>()
                              .Which.Model.Should().BeOfType<TelephoneNumberAPViewModel>()
                              .Subject.As<TelephoneNumberAPViewModel>();
            model.Id.Should().Be(id);
            model.EmailAddress.Should().Be("test@example.com");
            model.TelephoneNumber.Should().Be("123456789");
        }

        [Test]
        public async Task TelephoneNumber_Post_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
            var model = new TelephoneNumberAPViewModel();

            // Act
            var result = await _systemUnderTest.TelephoneNumber(model, Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task TelephoneNumber_Post_ReturnsViewResult_WithInvalidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new TelephoneNumberAPViewModel();
            _systemUnderTest.ModelState.AddModelError("TelephoneNumber", "The TelephoneNumber field is required.");

            // Act
            var result = await _systemUnderTest.TelephoneNumber(model, id);

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().Be("TelephoneNumber");
        }

        [Test]
        public async Task TelephoneNumber_Post_ReturnsRedirectToAction_WithValidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession();
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(frontendSchemeRegistrationSession);
            var model = new TelephoneNumberAPViewModel { TelephoneNumber = "123456789" };

            // Act
            var result = await _systemUnderTest.TelephoneNumber(model, id);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("ConfirmDetails");
        }

        [Test]
        public async Task ConfirmDetails_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.ConfirmDetails(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task ConfirmDetails_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var session = new FrontendSchemeRegistrationSession
            {
                NominatedApprovedPersonSession = new NominatedApprovedPersonSession
                {
                    TelephoneNumber = "123456789",
                    RoleInOrganisation = "Director"
                }
            };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
            var userData = new UserData { Organisations = new List<Organisation> { new Organisation { OrganisationType = "Companies House Company" } } };
            var claims = new List<Claim> { new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData)) };
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            _globalVariablesMock.Setup(g => g.Value).Returns(new GlobalVariables { BasePath = "/" });

            // Act
            var result = await _systemUnderTest.ConfirmDetails(id);

            // Assert
            var model = result.Should().BeOfType<ViewResult>()
                              .Which.Model.Should().BeOfType<ConfirmDetailsApprovedPersonViewModel>()
                              .Subject.As<ConfirmDetailsApprovedPersonViewModel>();
            model.Id.Should().Be(id);
            model.TelephoneNumber.Should().Be("123456789");
            model.RoleInOrganisation.Should().Be("Director");
            model.RoleChangeUrl.Should().Be("/" + PagePaths.RoleInOrganisation + "/" + id);
            model.TelephoneChangeUrl.Should().Be("/" + PagePaths.TelephoneNumberAP + "/" + id);
        }

        [Test]
        public async Task Declaration_ReturnsViewResult_WithCorrectModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var session = new FrontendSchemeRegistrationSession();
            var userData = new UserData { Organisations = new List<Organisation> { new Organisation { Name = "SomeOrg" } } };
            var claims = new List<Claim> { new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData)) };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            // Act
            var result = await _systemUnderTest.Declaration(id);

            // Assert
            var model = result.Should().BeOfType<ViewResult>()
                              .Which.Model.Should().BeOfType<DeclarationApprovedPersonViewModel>()
                              .Subject.As<DeclarationApprovedPersonViewModel>();
            model.Id.Should().Be(id);
            model.OrganisationName.Should().Be("SomeOrg");
        }

        [Test]
        public async Task Declaration_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _systemUnderTest.Declaration(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task Declaration_Post_ReturnsViewResult_WithInvalidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var model = new DeclarationApprovedPersonViewModel();
            var session = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession() };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
            var claims = CreateUserDataClaim(Guid.NewGuid(), string.Empty, string.Empty, OrganisationRoles.Producer);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            _systemUnderTest.ModelState.AddModelError("DeclarationFullName", "The DeclarationFullName field is required.");

            // Act
            var result = await _systemUnderTest.Declaration(model, id);

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().Be("Declaration");
        }

        [Test]
        public async Task Declaration_Post_ReturnsRedirectHome_WhenSessionIsNull()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
            var model = new DeclarationApprovedPersonViewModel { DeclarationFullName = "FirstName SecondName" };

            // Act
            var result = await _systemUnderTest.Declaration(model, Guid.NewGuid());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ControllerName.Should().Be("Landing");
        }

        [Test]
        public async Task Declaration_Post_ReturnsRedirectHome_WithValidModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var model = new DeclarationApprovedPersonViewModel { DeclarationFullName = "FirstName SecondName" };
            var session = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession() };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
            var claims = CreateUserDataClaim(userId, string.Empty, string.Empty, OrganisationRoles.Producer);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            // Act
            var result = await _systemUnderTest.Declaration(model, id);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("Get");
            _notificationServiceMock.Verify(m => m.ResetCache(_organisationId, userId), Times.Once);
        }

        private List<Claim> CreateUserDataClaim(Guid userId, string serviceRole, string enrolmentStatus, string organisationRole)
        {
            var userData = new UserData
            {
                Id = userId,
                ServiceRole = serviceRole,
                EnrolmentStatus = enrolmentStatus,
                Organisations = new List<Organisation>
                {
                new()
                {
                    Id = _organisationId,
                    OrganisationRole = organisationRole,
                    Name = "Some org",
                    OrganisationType = "Companies House Company"
                }
                }
            };

            return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData)),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", Guid.NewGuid().ToString())
        };
        }
    }
}
