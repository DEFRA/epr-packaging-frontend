using FrontendSchemeRegistration.Application.DTOs;

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
using Application.DTOs.Submission;
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
	 private readonly RegistrationApplicationSession _registrationApplicationSession = new RegistrationApplicationSession
	{
		LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
		RegistrationFeeCalculationDetails = null,
		ApplicationReferenceNumber = "",
		RegistrationReferenceNumber = "",
		SubmissionId = Guid.NewGuid(),
		RegistrationFeePaymentMethod = null,
		IsSubmitted = true,
		ApplicationStatus = ApplicationStatusType.NotStarted,
		RegistrationApplicationSubmittedComment = null,
		RegistrationApplicationSubmittedDate = null
	};
	
	[SetUp]
	public void Setup()
	{
		_userData = new UserData
		{
			Id = _userId,
			Organisations =
			[
				new Organisation
				{
					Id = _organisationId,
					Name = OrganisationName,
					OrganisationRole = OrganisationRole,
					OrganisationNumber = OrganisationNumber
				}
			]
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

		RegistrationApplicationService
			.Setup(s => s.GetRegistrationApplicationSession(
				It.IsAny<ISession>(),
				It.IsAny<Organisation>(), It.IsAny<bool?>()))
			.ReturnsAsync(_registrationApplicationSession);

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
			ApplicationReferenceNumber = string.Empty,
			RegistrationReferenceNumber = string.Empty,
			Notification = new NotificationViewModel
			{
				HasPendingNotification = false,
				HasNominatedNotification = false,
				NominatedEnrolmentId = string.Empty,
				NominatedApprovedPersonEnrolmentId = string.Empty
			},
            ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
            {
                AppReferenceNumber = null,
                IsResubmissionInProgress = null,
                IsResubmissionComplete = null,
            },
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

		RegistrationApplicationService
			.Setup(s => s.GetRegistrationApplicationSession(
				It.IsAny<ISession>(),
				It.IsAny<Organisation>(), It.IsAny<bool?>()))
			.ReturnsAsync(_registrationApplicationSession);

		var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

		result.ViewName.Should().Be(ViewName);

		result.Model.As<HomePageSelfManagedViewModel>().Notification.HasPendingNotification.Should().BeTrue();
		result.Model.As<HomePageSelfManagedViewModel>().Notification.HasNominatedNotification.Should().BeFalse();
	}

	[Test]
	public async Task GivenOnHomePageSelfManagedPage_WhenUserHasNominated_Notification_ThenHomePageComplianceSchemeViewModelWithNotificationReturned()
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

		RegistrationApplicationService
			.Setup(s => s.GetRegistrationApplicationSession(
				It.IsAny<ISession>(),
				It.IsAny<Organisation>(), It.IsAny<bool?>()))
			.ReturnsAsync(_registrationApplicationSession);

		var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

		result.ViewName.Should().Be(ViewName);

		result.Model.As<HomePageSelfManagedViewModel>().Notification.HasNominatedNotification.Should().BeTrue();
		result.Model.As<HomePageSelfManagedViewModel>().Notification.HasPendingNotification.Should().BeFalse();
		result.Model.As<HomePageSelfManagedViewModel>().Notification.NominatedEnrolmentId.Should().BeEquivalentTo(nominatedEnrolmentId);
	}

	[Test]
	public async Task GivenOnHomePageSelfManagedPage_WhenVisitHomePageSelfManagedHttpGetCalled_WithCallFromUsingComplianceScheme_ThenHomePageComplianceSchemeViewModelReturned()
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
				It.Is<FrontendSchemeRegistrationSession>(y => y.RegistrationSession.Journey.SequenceEqual(expectedJourney))),
			Times.Once);
	}
	
	    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_FileUploaded_Is_PendingState_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()));

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = null,
            ApplicationReferenceNumber = reference,
            RegistrationReferenceNumber = null,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        // Assert
        response.ViewName.Should().Be("HomePageSelfManaged");
        response.Model.Should()
            .BeOfType<HomePageSelfManagedViewModel>()
            .And
            .BeEquivalentTo(new HomePageSelfManagedViewModel
            {
                OrganisationName = OrganisationName,
                OrganisationNumber = "123 456",
                OrganisationRole = "Producer",
                ApplicationReferenceNumber = reference,
                FileUploadStatus = RegistrationTaskListStatus.Pending,
                PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
                AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                ApplicationStatus = ApplicationStatusType.FileUploaded,
                ResubmissionTaskListViewModel = new()
            });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()));

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            RegistrationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        // Assert
        response.ViewName.Should().Be("HomePageSelfManaged");
        response.Model.Should()
            .BeOfType<HomePageSelfManagedViewModel>()
            .And
            .BeEquivalentTo(new HomePageSelfManagedViewModel
            {
                OrganisationName = OrganisationName,
                ApplicationReferenceNumber = reference,
                RegistrationReferenceNumber = reference,
                OrganisationNumber = "123 456",
                OrganisationRole = "Producer",
                FileUploadStatus = RegistrationTaskListStatus.Completed,
                PaymentViewStatus = RegistrationTaskListStatus.Completed,
                AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                ResubmissionTaskListViewModel = new()
            });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_ApplicationSubmittedToRegulator_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()));

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            RegistrationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;

        // Assert
        response.ViewName.Should().Be("HomePageSelfManaged");
        response.Model.Should()
            .BeOfType<HomePageSelfManagedViewModel>()
            .And
            .BeEquivalentTo(new HomePageSelfManagedViewModel
            {
                OrganisationName = OrganisationName,
                OrganisationNumber = "123 456",
                OrganisationRole = "Producer",
                ApplicationReferenceNumber = reference,
                RegistrationReferenceNumber = reference,
                FileUploadStatus = RegistrationTaskListStatus.Completed,
                PaymentViewStatus = RegistrationTaskListStatus.Completed,
                AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                ResubmissionTaskListViewModel = new()
            });
    }
}