namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.ViewModels;

[TestFixture]
public class HomePageSelfManagedTests : FrontendSchemeRegistrationTestBase
{
    private const string ViewName = "HomePageSelfManaged";
    private const string OrganisationName = "Test Organisation";
    private const string OrganisationNumber = "123456";
    private const string OrganisationRole = OrganisationRoles.Producer;
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private UserData _userData;

    [SetUp]
    public void Setup()
    {
        _userData = new UserData
        {
            Id = _userId,
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                    Name = OrganisationName,
                    OrganisationRole = OrganisationRole,
                    OrganisationNumber = OrganisationNumber
                }
            }
        };

        SetupBase(_userData);
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession();

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    [Test]
    public async Task VisitHomePageSelfManaged_RedirectsToComplianceSchemeMemberLanding_WhenProducerIsLinkedWithAComplianceScheme()
    {
        // Arrange
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync(new ProducerComplianceSchemeDto());
        AuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<HttpContext>(), PolicyConstants.EprSelectSchemePolicy))
            .ReturnsAsync(AuthorizationResult.Success);

        // Act
        var result = await SystemUnderTest.VisitHomePageSelfManaged() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));
        result.ControllerName.Should().Be(nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName());
    }

    [Test]
    [TestCase(ServiceRoles.ApprovedPerson, true)]
    [TestCase(ServiceRoles.DelegatedPerson, true)]
    [TestCase(ServiceRoles.BasicUser, false)]
    public async Task VisitHomePageSelfManaged_ReturnsHomePageSelfManagedViewWithCorrectViewModel_WhenProducerIsNotLinkedWithAComplianceScheme(
        string serviceRole,
        bool expectedCanSelectComplianceSchemeValue)
    {
        // Arrange
        AuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<HttpContext>(), PolicyConstants.EprSelectSchemePolicy))
            .ReturnsAsync(AuthorizationResult.Success);
        _userData.ServiceRole = serviceRole;
        SetupBase(_userData);

        var notificationList = new List<NotificationDto>();
        NotificationService.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(notificationList));

        // Act
        var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.As<HomePageSelfManagedViewModel>().Should().BeEquivalentTo(new HomePageSelfManagedViewModel
        {
            OrganisationName = OrganisationName,
            OrganisationNumber = OrganisationNumber.ToReferenceNumberFormat(),
            OrganisationRole = OrganisationRole,
            CanSelectComplianceScheme = expectedCanSelectComplianceSchemeValue,
            Notification = new NotificationViewModel
            {
                HasPendingNotification = false,
                HasNominatedNotification = false,
                NominatedEnrolmentId = string.Empty,
                NominatedApprovedPersonEnrolmentId = string.Empty
            }
        });
    }

    [Test]
    public async Task GivenOnHomePageSelfManagedPage_WhenUserHasPendingNotification_ThenHomePageComplianceSchemeViewModelWithNotificationReturned()
    {
        var notificationList = new List<NotificationDto>
        {
            new()
            {
                Type = NotificationTypes.Packaging.DelegatedPersonPendingApproval
            }
        };
        NotificationService.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(notificationList));

        var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        result.ViewName.Should().Be(ViewName);

        result.Model.As<HomePageSelfManagedViewModel>().Notification.HasPendingNotification.Should().BeTrue();
        result.Model.As<HomePageSelfManagedViewModel>().Notification.HasNominatedNotification.Should().BeFalse();
    }

    [Test]
    public async Task GivenOnHomePageSelfManagedPage_WhenUserHasNominatedNotifHomeication_ThenHomePageComplianceSchemeViewModelWithNotificationReturned()
    {
        var notificationList = new List<NotificationDto>();
        var nominatedEnrolmentId = Guid.NewGuid().ToString();
        var notificationData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("EnrolmentId", nominatedEnrolmentId)
        };
        notificationList.Add(new NotificationDto
        {
            Type = NotificationTypes.Packaging.DelegatedPersonNomination,
            Data = notificationData
        });
        NotificationService.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(notificationList));

        var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        result.ViewName.Should().Be(ViewName);

        result.Model.As<HomePageSelfManagedViewModel>().Notification.HasNominatedNotification.Should().BeTrue();
        result.Model.As<HomePageSelfManagedViewModel>().Notification.HasPendingNotification.Should().BeFalse();
        result.Model.As<HomePageSelfManagedViewModel>().Notification.NominatedEnrolmentId.Should().BeEquivalentTo(nominatedEnrolmentId);
    }

    [Test]
    public async Task
        GivenOnHomePageSelfManagedPage_WhenVisitHomePageSelfManagedHttpGetCalled_WithCallFromUsingComplianceScheme_ThenHomePageComplianceSchemeViewModelReturned()
    {
        // Act
        var result = await SystemUnderTest.HomePageSelfManaged() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.UsingAComplianceScheme));
        var expectedJourney = new List<string>
        {
            PagePaths.HomePageSelfManaged,
            PagePaths.UsingAComplianceScheme
        };
        SessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(x => x.RegistrationSession.Journey.SequenceEqual(expectedJourney))),
            Times.Once);
    }
}