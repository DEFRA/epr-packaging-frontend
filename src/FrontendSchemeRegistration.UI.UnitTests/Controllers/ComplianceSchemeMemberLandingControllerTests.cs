namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;
using UI.ViewModels;

public class ComplianceSchemeMemberLandingControllerTests
{
    private const string ComplianceSchemeName = "Compliance Scheme Ltd";
    private const string OrganisationName = "Organistation Ltd";
    private const string OrganisationNumber = "123456";
    private readonly Guid _organisationId = Guid.NewGuid();

    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManager = new();
    private readonly Mock<IComplianceSchemeService> _complianceSchemeService = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly NullLogger<ComplianceSchemeMemberLandingController> _nullLogger = new();
    private ComplianceSchemeMemberLandingController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        var userData = new UserData
        {
            Id = _organisationId,
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                    Name = OrganisationName,
                    OrganisationRole = "ComplianceScheme",
                    OrganisationNumber = OrganisationNumber
                }
            },
            ServiceRole = ServiceRoles.ApprovedPerson
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(userData))
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _systemUnderTest = new ComplianceSchemeMemberLandingController(_sessionManager.Object, _complianceSchemeService.Object, _notificationServiceMock.Object, _nullLogger)
        {
            ControllerContext =
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToVisitHomePageSelfManaged_WhenCurrentComplianceSchemeIsNull()
    {
        // Arrange
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        _complianceSchemeService
            .Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>()))
            .ReturnsAsync((ProducerComplianceSchemeDto)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(FrontendSchemeRegistrationController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenCurrentComplianceSchemeIsNotNull()
    {
        // Arrange
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        _complianceSchemeService
            .Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>()))
            .ReturnsAsync(new ProducerComplianceSchemeDto { ComplianceSchemeName = ComplianceSchemeName });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("ComplianceSchemeMemberLanding");
        result.Model.Should().BeEquivalentTo(new ComplianceSchemeMemberLandingViewModel
        {
            ComplianceSchemeName = ComplianceSchemeName,
            OrganisationName = OrganisationName,
            OrganisationId = _organisationId,
            CanManageComplianceScheme = true,
            ServiceRole = ServiceRoles.ApprovedPerson,
            OrganisationNumber = OrganisationNumber.ToReferenceNumberFormat()
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenUserHasPendingNotification()
    {
        // Arrange

        var notificationDtoList = new List<NotificationDto>
        {
            new NotificationDto
            {
                Type = NotificationTypes.Packaging.DelegatedPersonPendingApproval,
                Data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("EnrolmentId", Guid.NewGuid().ToString())
                }
            }
        };

        _notificationServiceMock
            .Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        _complianceSchemeService
            .Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>()))
            .ReturnsAsync(new ProducerComplianceSchemeDto { ComplianceSchemeName = ComplianceSchemeName });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("ComplianceSchemeMemberLanding");
        result.Model.Should().BeEquivalentTo(new ComplianceSchemeMemberLandingViewModel
        {
            ComplianceSchemeName = ComplianceSchemeName,
            OrganisationName = OrganisationName,
            OrganisationId = _organisationId,
            CanManageComplianceScheme = true,
            ServiceRole = ServiceRoles.ApprovedPerson,
            OrganisationNumber = OrganisationNumber.ToReferenceNumberFormat(),
            Notification = new NotificationViewModel
            {
                HasNominatedNotification = false,
                HasPendingNotification = true,
                NominatedEnrolmentId = string.Empty,
                NominatedApprovedPersonEnrolmentId = string.Empty
            }
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenUserHasNominatedNotification()
    {
        // Arrange
        var notificationDtoList = new List<NotificationDto>
        {
            new NotificationDto
            {
                Type = NotificationTypes.Packaging.DelegatedPersonNomination,
                Data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("EnrolmentId", Guid.NewGuid().ToString())
                }
            }
        };

        _notificationServiceMock
            .Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        _complianceSchemeService
            .Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>()))
            .ReturnsAsync(new ProducerComplianceSchemeDto { ComplianceSchemeName = ComplianceSchemeName });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("ComplianceSchemeMemberLanding");
        result.Model.Should().BeEquivalentTo(new ComplianceSchemeMemberLandingViewModel
        {
            ComplianceSchemeName = ComplianceSchemeName,
            OrganisationName = OrganisationName,
            OrganisationId = _organisationId,
            CanManageComplianceScheme = true,
            ServiceRole = ServiceRoles.ApprovedPerson,
            OrganisationNumber = OrganisationNumber.ToReferenceNumberFormat(),
            Notification = new NotificationViewModel
            {
                HasNominatedNotification = true,
                HasPendingNotification = false,
                NominatedEnrolmentId = notificationDtoList.First().Data.ToList().FirstOrDefault().Value,
                NominatedApprovedPersonEnrolmentId = string.Empty,
            }
        });
    }
}