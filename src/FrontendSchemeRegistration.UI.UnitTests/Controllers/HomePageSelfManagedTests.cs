using FrontendSchemeRegistration.Application.DTOs;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using Application.DTOs.Submission;
using Application.Enums;
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
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.ViewModels;
using UI.ViewModels.Shared;

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

        var submissionPeriod = new SubmissionPeriod
        {
            DataPeriod = "Data period 3",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Today.AddDays(10),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        };

        var notificationList = new List<NotificationDto>();
        NotificationService.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(notificationList));

        RegistrationApplicationService
            .Setup(s => s.GetRegistrationApplicationSession(
                It.IsAny<ISession>(),
                It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(_registrationApplicationSession);

        ResubmissionApplicationService.Setup(x => x.GetActiveSubmissionPeriod()).ReturnsAsync(submissionPeriod);

        var registrationApplicationPerYear = new List<RegistrationApplicationViewModel>()
        {
            new RegistrationApplicationViewModel {
            ApplicationStatus = _registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = _registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = _registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = _registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = _registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = _registrationApplicationSession.IsResubmission,
           }
        };

        RegistrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        // Act
        var result = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;
        var complianceYear = ((HomePageSelfManagedViewModel)result.Model).ComplianceYear;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.As<HomePageSelfManagedViewModel>().Should().BeEquivalentTo(new HomePageSelfManagedViewModel
        {
            OrganisationName = OrganisationName,
            OrganisationNumber = OrganisationNumber.ToReferenceNumberFormat(),
            OrganisationRole = OrganisationRole,
            CanSelectComplianceScheme = expectedCanSelectComplianceSchemeValue,
            PackagingResubmissionPeriod = submissionPeriod,
            RegistrationApplicationsPerYear = new List<RegistrationApplicationViewModel>()
            {
                new RegistrationApplicationViewModel
                {
                    ApplicationReferenceNumber = string.Empty,
                    RegistrationReferenceNumber = string.Empty,
                }
            },
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
            ComplianceYear = complianceYear
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
                It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
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
                It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
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

        var submissionPeriod = new SubmissionPeriod
        {
            DataPeriod = "Data period 3",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Today.AddDays(10),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        ResubmissionApplicationService.Setup(x => x.GetActiveSubmissionPeriod()).ReturnsAsync(submissionPeriod);

        var registrationApplicationPerYear = new List<RegistrationApplicationViewModel>()
        {
            new RegistrationApplicationViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        RegistrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;
        var complianceYear = ((HomePageSelfManagedViewModel)response.Model).ComplianceYear;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationViewModel>()
                {
                    new RegistrationApplicationViewModel
                    {
                        ApplicationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Pending,
                        PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                        ApplicationStatus = ApplicationStatusType.FileUploaded,
                    }
                },
                ResubmissionTaskListViewModel = new(),
                PackagingResubmissionPeriod = submissionPeriod,
                ComplianceYear = complianceYear
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

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        var submissionPeriod = new SubmissionPeriod
        {
            DataPeriod = "Data period 3",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Today.AddDays(10),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        };

        ResubmissionApplicationService.Setup(x => x.GetActiveSubmissionPeriod()).ReturnsAsync(submissionPeriod);

        var registrationApplicationPerYear = new List<RegistrationApplicationViewModel>()
        {
            new RegistrationApplicationViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        RegistrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;
        var complianceYear = ((HomePageSelfManagedViewModel)response.Model).ComplianceYear;

        // Assert
        response.ViewName.Should().Be("HomePageSelfManaged");
        response.Model.Should()
            .BeOfType<HomePageSelfManagedViewModel>()
            .And
            .BeEquivalentTo(new HomePageSelfManagedViewModel
            {
                RegistrationApplicationsPerYear = new List<RegistrationApplicationViewModel>()
                {
                    new RegistrationApplicationViewModel
                    {
                        ApplicationReferenceNumber = reference,
                        RegistrationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Completed,
                        PaymentViewStatus = RegistrationTaskListStatus.Completed,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
                        ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                    }
                },
                OrganisationName = OrganisationName,
                OrganisationNumber = "123 456",
                OrganisationRole = "Producer",

                ResubmissionTaskListViewModel = new(),
                PackagingResubmissionPeriod = submissionPeriod,
                ComplianceYear = complianceYear
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

        var submissionPeriod = new SubmissionPeriod
        {
            DataPeriod = "January to June 2025",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Parse("2025-07-01"),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(),It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        ResubmissionApplicationService.Setup(x => x.GetActiveSubmissionPeriod()).ReturnsAsync(submissionPeriod);

        var registrationApplicationPerYear = new List<RegistrationApplicationViewModel>()
        {
            new RegistrationApplicationViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        RegistrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        ResubmissionApplicationService.Setup(x => x.GetActiveSubmissionPeriod()).ReturnsAsync(submissionPeriod);

        // Act
        var response = await SystemUnderTest.VisitHomePageSelfManaged() as ViewResult;
        var complianceYear = ((HomePageSelfManagedViewModel)response.Model).ComplianceYear;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationViewModel>()
                {
                    new RegistrationApplicationViewModel
                    {
                        ApplicationReferenceNumber = reference,
                        RegistrationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Completed,
                        PaymentViewStatus = RegistrationTaskListStatus.Completed,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
                        ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
                    }
                },
                ResubmissionTaskListViewModel = new(),
                PackagingResubmissionPeriod = submissionPeriod,
                ComplianceYear = complianceYear
            });
    }
}