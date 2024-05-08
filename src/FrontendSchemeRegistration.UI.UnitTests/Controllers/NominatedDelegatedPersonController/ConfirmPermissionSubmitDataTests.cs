namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.NominatedDelegatedPersonController;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Application.DTOs;
using Application.Services;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class ConfirmPermissionSubmitDataTests
{
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<IAccountServiceApiClient> _accountServiceApiClient = new();
    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private IRoleManagementService _roleManagementService;

    private Guid _enrolmentId = Guid.NewGuid();
    private Guid _organisationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private UserData _userData;
    private NominatedDelegatedPersonController _systemUnderTest;

    [SetUp]
    public void Setup()
    {
        IOptions<GlobalVariables> globalVariables = Options.Create(new GlobalVariables { BasePath = "/report-data" });

        _roleManagementService = new RoleManagementService(_accountServiceApiClient.Object, NullLogger<RoleManagementService>.Instance);
        _systemUnderTest = new NominatedDelegatedPersonController(_sessionManagerMock.Object, globalVariables, _roleManagementService, _notificationService.Object);
        _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;

        _userData = new UserData
        {
            Email = "test@test.com",
            Organisations = new List<Organisation> { new() { Id = _organisationId } }
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(_userData)),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", _userId.ToString())
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenHttpGetIsCalledAndSessionIsNull_ThenRedirectHome()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(() => null);

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(_enrolmentId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    [TestCase(null)]
    [TestCase("Nominee Full Name")]
    public async Task ConfirmPermissionSubmitData_WhenHttpGetIsCalled_ThenNomineeFullNameGetsValueFromSession(string nomineeFullName)
    {
        // Arrange
        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Nominator Full Name",
                NomineeFullName = nomineeFullName
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(_enrolmentId) as ViewResult;

        // Assert
        result.Should().NotBeNull();

        var model = result.Model as NominationAcceptanceModel;
        model.Should().NotBeNull();

        model.NomineeFullName.Should().Be(nomineeFullName);
        model.NominatorFullName.Should().Be("Nominator Full Name");
        model.EnrolmentId.Should().Be(_enrolmentId);
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenValidHttpPutIsCalled_ThenSessionGetsUpdated_AndRedirectToLandingPage()
    {
        // Arrange
        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Nominator Full Name"
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var model = new NominationAcceptanceModel
        {
            EnrolmentId = _enrolmentId,
            NominatorFullName = "Nominator Full Name",
            NomineeFullName = "Nominee Full Name"
        };

        _accountServiceApiClient.Setup(x => x.PutAsJsonAsync(
                _organisationId,
                $"enrolments/{model.EnrolmentId}/delegated-person-acceptance?serviceKey=Packaging", It.IsAny<AcceptNominationRequest>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _notificationService.Invocations.Clear();

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(model, _enrolmentId) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();

        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    session => session.NominatedDelegatedPersonSession.NominatorFullName == model.NominatorFullName &&
                    session.NominatedDelegatedPersonSession.NomineeFullName == model.NomineeFullName)),
            Times.Once);

        _notificationService.Verify(x => x.ResetCache(_organisationId, _userId), Times.Once);
    }

    [Test]
    public void ConfirmPermissionSubmitData_WhenHttpPutIsUnsuccessful_ThenThrowHttpRequestException()
    {
        // Arrange
        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Nominator Full Name"
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var model = new NominationAcceptanceModel
        {
            EnrolmentId = _enrolmentId,
            NominatorFullName = "Nominator Full Name",
            NomineeFullName = string.Empty
        };

        _accountServiceApiClient.Setup(x => x.PutAsJsonAsync(
                _organisationId,
                "enrolments/{model.EnrolmentId}/delegated-person-acceptance?serviceKey=Packaging", It.IsAny<AcceptNominationRequest>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var act = async () => await _systemUnderTest.ConfirmPermissionSubmitData(model, _enrolmentId);

        // Assert
        act.Should().ThrowAsync<HttpRequestException>();

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenHttpPostIsCalledAndSessionIsNull_ThenRedirectHome()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(() => null);

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(null, _enrolmentId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenHttpPostIsCalledAndOrganisationIdNull_ThenRedirectHome()
    {
        // Arrange
        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Nominator Full Name",
                NomineeFullName = "Nominee Full Name"
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(new UserData { Email = "test@test.com" })),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", _userId.ToString())
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(null, _enrolmentId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenHttpPostModelIsInvalid_ThenReturnView()
    {
        // Arrange
        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Ses Nominator Full Name",
                NomineeFullName = "Ses Nominator Full Name",
                OrganisationName = "Ses Organisation Name"
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        _systemUnderTest.ModelState.AddModelError("some", "error");

        var model = new NominationAcceptanceModel
        {
            EnrolmentId = _enrolmentId,
            NominatorFullName = "Nominator Full Name",
            NomineeFullName = string.Empty
        };

        // Act
        var result = await _systemUnderTest.ConfirmPermissionSubmitData(model, _enrolmentId) as ViewResult;

        // Assert
        result.ViewName.Should().Be(null);
        result.ViewData.Model.Should().BeEquivalentTo(new NominationAcceptanceModel
        {
            EnrolmentId = _enrolmentId,
            NominatorFullName = session.NominatedDelegatedPersonSession.NominatorFullName,
            NomineeFullName = session.NominatedDelegatedPersonSession.NomineeFullName,
            OrganisationName = session.NominatedDelegatedPersonSession.OrganisationName
        });
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenNomineeFullNameIsMissing_ThenTheValidationFails()
    {
        // Arrange
        var model = new NominationAcceptanceModel
        {
            EnrolmentId = Guid.NewGuid(),
            NominatorFullName = "Nominator Full Name",
            NomineeFullName = null!
        };

        var context = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        // Act
        var isModelStateValid = Validator.TryValidateObject(model, context, validationResults, true);

        // Assert
        isModelStateValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Be("ConfirmPermissionSubmitData.FullNameError");
    }

    [Test]
    public async Task ConfirmPermissionSubmitData_WhenNomineeFullNameExceedsMaximumLength_ThenTheValidationFails()
    {
        // Arrange
        var model = new NominationAcceptanceModel
        {
            EnrolmentId = Guid.NewGuid(),
            NominatorFullName = "Nominator Full Name",
            NomineeFullName = new string('*', 201)
        };

        var context = new ValidationContext(model, null, null);
        var validationResults = new List<ValidationResult>();

        // Act
        var isModelStateValid = Validator.TryValidateObject(model, context, validationResults, true);

        // Assert
        isModelStateValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Be("ConfirmPermissionSubmitData.FullNameMaxLengthError");
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpPostCalled_ThenRedirectToConfirmPermissionSubmitData_AndUpdateSession()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new NominatedDelegatedPersonSession { Journey = new List<string>() } };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _sessionManagerMock.Invocations.Clear();

        var model = new TelephoneNumberViewModel { TelephoneNumber = "07904123456" };

        // Act
        var result = await _systemUnderTest.TelephoneNumber(model, _enrolmentId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        (result as RedirectToActionResult).ActionName.Should().Be(nameof(NominatedDelegatedPersonController.ConfirmPermissionSubmitData));
        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }
}