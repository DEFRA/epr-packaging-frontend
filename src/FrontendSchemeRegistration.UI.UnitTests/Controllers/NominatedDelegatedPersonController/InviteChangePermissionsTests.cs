namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.NominatedDelegatedPersonController;

using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using UI.Controllers;

[TestFixture]
public class InviteChangePermissionsTests
{
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IRoleManagementService> _roleManagementServiceMock = new();

    private Guid _enrolmentId = Guid.NewGuid();
    private Guid _organisationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private UserData _userData;
    private NominatedDelegatedPersonController _systemUnderTest;

    [SetUp]
    public void Setup()
    {
        IOptions<GlobalVariables> globalVariables = Options.Create(new GlobalVariables { BasePath = "/report-data" });

        _systemUnderTest = new NominatedDelegatedPersonController(
            _sessionManagerMock.Object,
            globalVariables,
            _roleManagementServiceMock.Object,
            _notificationServiceMock.Object);

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
    public async Task InviteChangePermissions_WhenSessionIsNull_ThenRedirectHome()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(() => null);

        // Act
        var result = await _systemUnderTest.InviteChangePermissions(Guid.NewGuid()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task InviteChangePermissions_WhenSessionExists_ThenReturnsCorrectView()
    {
        // Arrange
        var id = Guid.NewGuid();

        var delegatedPersonNominatorDto = new DelegatedPersonNominatorDto
        {
            FirstName = "Test FirstName",
            LastName = "Test Surname",
            OrganisationName = "Test OrganisationName"
        };

        FrontendSchemeRegistrationSession session = new()
        {
            NominatedDelegatedPersonSession = new()
            {
                Journey = new(),
                NominatorFullName = "Test NominatorFullName",
                NomineeFullName = "Test NomineeFullName"
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        _roleManagementServiceMock.Setup(x => x.GetDelegatedPersonNominator(id, _organisationId))
            .ReturnsAsync(delegatedPersonNominatorDto);

        // Act
        var result = await _systemUnderTest.InviteChangePermissions(id) as ViewResult;

        // Assert
        result.ViewName.Should().Be(nameof(NominatedDelegatedPersonController.InviteChangePermissions));
        result.ViewData.Model.Should().BeEquivalentTo(new InviteChangePermissionsViewModel
        {
            Id = id,
            Firstname = delegatedPersonNominatorDto.FirstName,
            Lastname = delegatedPersonNominatorDto.LastName,
            OrganisationName = delegatedPersonNominatorDto.OrganisationName
        });

        session.NominatedDelegatedPersonSession.Journey.Count.Should().Be(3);
        session.NominatedDelegatedPersonSession.Journey[0].Should().Be(string.Empty);
        session.NominatedDelegatedPersonSession.Journey[1].Should().Be($"{PagePaths.InviteChangePermissions}/{id}");
        session.NominatedDelegatedPersonSession.Journey[2].Should().Be($"{PagePaths.TelephoneNumber}/{id}");

        session.NominatedDelegatedPersonSession.NominatorFullName.Should().Be($"{delegatedPersonNominatorDto.FirstName} {delegatedPersonNominatorDto.LastName}");
        session.NominatedDelegatedPersonSession.OrganisationName.Should().Be(delegatedPersonNominatorDto.OrganisationName);

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }
}