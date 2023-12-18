namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.ViewModels;

[TestFixture]
public class LandingPageTests : FrontendSchemeRegistrationTestBase
{
    private const string OrganisationName = "Acme Org Ltd";
    private readonly Guid _organisationId = Guid.NewGuid();
    private UserData _userData;

    [SetUp]
    public void Setup()
    {
        _userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Name = OrganisationName,
                    Id = _organisationId,
                    OrganisationRole = "Producer"
                }
            }
        };

        SetupBase(_userData);

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.LandingPage,
                },
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    [Test]
    public async Task GivenOnLandingPage_WhenLandingPageHttpGetCalled_ThenLandingPageViewModelReturned()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);

        // Act
        var result = await SystemUnderTest.LandingPage();

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<LandingPageViewModel>();

        var viewModel = (result as ViewResult).Model as LandingPageViewModel;
        viewModel.OrganisationId.Should().Be(_organisationId);
        viewModel.OrganisationName.Should().Be(OrganisationName);
    }

    [Test]
    public async Task GivenOnLandingPage_WhenUserHasNominatedNotification_ThenCorrectViewModelReturned()
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

        NotificationService
            .Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);

        // Act
        var result = await SystemUnderTest.LandingPage();

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<LandingPageViewModel>();

        var viewModel = (result as ViewResult).Model as LandingPageViewModel;
        viewModel.Notification.NominatedEnrolmentId.Should().Be(notificationDtoList.First().Data.FirstOrDefault().Value);
        viewModel.Notification.HasNominatedNotification.Should().BeTrue();
        viewModel.Notification.HasPendingNotification.Should().BeFalse();
    }

    [Test]
    public async Task GivenOnLandingPage_WhenUserHasPendingNotification_ThenCorrectViewModelReturned()
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

        NotificationService
            .Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);

        // Act
        var result = await SystemUnderTest.LandingPage();

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<LandingPageViewModel>();

        var viewModel = (result as ViewResult).Model as LandingPageViewModel;
        viewModel.Notification.NominatedEnrolmentId.Should().Be(string.Empty);
        viewModel.Notification.HasNominatedNotification.Should().BeFalse();
        viewModel.Notification.HasPendingNotification.Should().BeTrue();
    }

    [Test]
    public async Task GivenOnLandingPage_WhenLandingPageHttpPostCalled_ThenRedirectToUsingComplianceScheme_AndUpdateSession()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);

        // Act
        var result = await SystemUnderTest.LandingPage(new LandingPageViewModel
        {
            OrganisationId = _organisationId,
            OrganisationName = OrganisationName,
        });

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.UsingAComplianceScheme));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
        FrontEndSchemeRegistrationSession.RegistrationSession.Journey.Should().BeEquivalentTo(new List<string>
        {
            PagePaths.LandingPage
        });
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney.Should().BeFalse();
    }

    [Test]
    public async Task GivenOnLandingPage_WhenLandingPageHttpPostCalledAndModelHasError_ThenReturnView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);
        SystemUnderTest.ControllerContext.ModelState.TryAddModelError("Test", "Test");

        // Act
        var result = await SystemUnderTest.LandingPage(new LandingPageViewModel
        {
            OrganisationId = _organisationId,
            OrganisationName = OrganisationName,
        });

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Test]
    public async Task GivenOnLandingPage_WhenNotificationListIsNotNull_ThenReturnView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync((ProducerComplianceSchemeDto)null);
        SystemUnderTest.ControllerContext.ModelState.TryAddModelError("Test", "Test");
        var listOfNotificationDto = new List<NotificationDto>
        {
            new()
            {
                Type = NotificationTypes.Packaging.DelegatedPersonPendingApproval,
                Data = new List<KeyValuePair<string, string>>
                {
                    new("EnrolmentId", Guid.NewGuid().ToString())
                }
            }
        };

        NotificationService.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(listOfNotificationDto);
        // Act
        var result = await SystemUnderTest.LandingPage(new LandingPageViewModel
        {
            OrganisationId = _organisationId,
            OrganisationName = OrganisationName,
        }) as ViewResult;

        // Assert
        result.Should().BeOfType<ViewResult>();
        var landingPageViewModel = result.Model as LandingPageViewModel;
        landingPageViewModel.Notification.HasNominatedNotification.Should().BeFalse();
        landingPageViewModel.Notification.HasPendingNotification.Should().BeTrue();
    }

    [Test]
    public async Task GivenGetLandingPage_WhenProducerHasComplianceScheme_ThenManageComplianceSchemeReturned()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>()))
            .ReturnsAsync(new ProducerComplianceSchemeDto());
        AuthorizationService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).ReturnsAsync(AuthorizationResult.Success);

        // Act
        var result = await SystemUnderTest.LandingPage();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be("Get");
    }
}