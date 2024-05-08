namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.NominatedDelegatedPersonController;

using System.Security.Claims;
using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using UI.ViewModels;

[TestFixture]
public class TelephoneNumberTests
{
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<IRoleManagementService> _roleManagementService = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private Guid _enrolmentId = Guid.NewGuid();
    private UserData _userData;

    protected Mock<ISessionManager<FrontendSchemeRegistrationSession>> SessionManagerMock { get; private set; }

    protected NominatedDelegatedPersonController SystemUnderTest { get; set; }

    [SetUp]
    public void Setup()
    {
        IOptions<GlobalVariables> globalVariablesOptions = Options.Create(
           new GlobalVariables
           {
               BasePath = "/report-data"
           });

        SessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        SystemUnderTest = new NominatedDelegatedPersonController(SessionManagerMock.Object, globalVariablesOptions, _roleManagementService.Object, _notificationService.Object);
        SystemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;

        _userData = new UserData
        {
            Email = "test@test.com"
        };

        var claims = new List<Claim>();
        claims.Add(new(ClaimTypes.UserData, JsonConvert.SerializeObject(_userData)));

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpGetCalledAndSessionIsNull_ThenRedirectHome()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(() => null);

        // Act
        var result = await SystemUnderTest.TelephoneNumber(_enrolmentId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpGetCalled_ThenTelephoneViewModelReturned()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new NominatedDelegatedPersonSession { Journey = new List<string>() } };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = await SystemUnderTest.TelephoneNumber(_enrolmentId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<TelephoneNumberViewModel>();

        var viewModel = (result as ViewResult).Model as TelephoneNumberViewModel;
        viewModel.EmailAddress.Should().Be(_userData.Email);
        viewModel.EnrolmentId.Should().Be(_enrolmentId);
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpGetCalledAgain_ThenTelephoneViewModelReturnedWithNumberSet()
    {
        // Arrange
        string expectedNumber = "07904123456";
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new NominatedDelegatedPersonSession { TelephoneNumber = expectedNumber, Journey = new List<string>() } };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = await SystemUnderTest.TelephoneNumber(_enrolmentId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<TelephoneNumberViewModel>();

        var viewModel = (result as ViewResult).Model as TelephoneNumberViewModel;
        viewModel.TelephoneNumber.Should().Be(expectedNumber);
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpPostCalled_ThenRedirectToConfirmPermissionSubmitData_AndUpdateSession()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new NominatedDelegatedPersonSession { Journey = new List<string>() } };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        var model = new TelephoneNumberViewModel { TelephoneNumber = "07904123456" };

        // Act
        var result = await SystemUnderTest.TelephoneNumber(model, _enrolmentId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(NominatedDelegatedPersonController.ConfirmPermissionSubmitData));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpPostCalledAndSessionIsNull_ThenRedirectHome()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(() => null);
        var model = new TelephoneNumberViewModel { TelephoneNumber = "07904123456" };

        // Act
        var result = await SystemUnderTest.TelephoneNumber(model, _enrolmentId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task TelephoneNumber_WhenHttpPostCalledWithInvalidData_ThenReturnView()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession { NominatedDelegatedPersonSession = new NominatedDelegatedPersonSession { Journey = new List<string>() } };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        var model = new TelephoneNumberViewModel { TelephoneNumber = "test" };

        SystemUnderTest.ModelState.AddModelError("number", "error");

        // Act
        var result = await SystemUnderTest.TelephoneNumber(model, _enrolmentId) as ViewResult;

        // Assert
        result.ViewName.Should().Be(null);
        result.ViewData.Model.Should().BeEquivalentTo(new TelephoneNumberViewModel
        {
            EnrolmentId = _enrolmentId,
            TelephoneNumber = "test",
            EmailAddress = _userData.Email
        });
    }
}