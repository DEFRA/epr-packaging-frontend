namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using Application.DTOs.Submission;
using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class ComplianceSchemeLandingControllerTests
{
    private const string OrganisationName = "Acme Org Ltd";

    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly Guid _complianceSchemeOneId = Guid.NewGuid();
    private readonly Guid _complianceSchemeTwoId = Guid.NewGuid();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly NullLogger<ComplianceSchemeLandingController> _nullLogger = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IComplianceSchemeService> _complianceSchemeServiceMock = new();
    private readonly Mock<ISubmissionService> _submissionService = new();

    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = "Data period 1",
            Deadline = DateTime.Today,
            StartMonth = "January",
            EndMonth = "June"
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 2",
            Deadline = DateTime.Today.AddDays(5),
            StartMonth = "July",
            EndMonth = "December"
        }
    };

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock = new();
    private ComplianceSchemeLandingController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                    Name = OrganisationName,
                    OrganisationRole = "ComplianceScheme",
                    OrganisationNumber = "552555"
                }
            },
            ServiceRole = "Approved Person"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData))
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _notificationServiceMock.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>()));
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

        _systemUnderTest = new ComplianceSchemeLandingController(
            _sessionManagerMock.Object,
            _complianceSchemeServiceMock.Object,
            _notificationServiceMock.Object,
            _submissionService.Object,
            _nullLogger,
            Options.Create(new GlobalVariables { SubmissionPeriods = _submissionPeriods }))
        {
            ControllerContext = { HttpContext = _httpContextMock.Object }
        };
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenSelectedComplianceSchemeDoesNotExistInSession()
    {
        // Arrange
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithoutSelectedScheme());

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);
        
        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                OrganisationName = OrganisationName,
                CurrentComplianceSchemeId = _complianceSchemeOneId,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                ApplicationStatus = ApplicationStatusType.NotStarted,
                ApplicationReferenceNumber = string.Empty
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenSelectedComplianceSchemeExistsInSession()
    {
        // Arrange
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithSelectedScheme());

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                ApplicationStatus = ApplicationStatusType.NotStarted,
                ApplicationReferenceNumber = string.Empty
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_FileUploaded_Is_PendingState_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithSelectedScheme());

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = null,
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                ApplicationReferenceNumber = reference,
                FileUploadStatus = RegistrationTaskListStatus.Pending,
                PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
                AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                ApplicationStatus = ApplicationStatusType.FileUploaded
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithSelectedScheme());

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                ApplicationReferenceNumber = reference,
                FileUploadStatus = RegistrationTaskListStatus.Completed,
                PaymentViewStatus = RegistrationTaskListStatus.Completed,
                AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_ApplicationSubmittedToRegulator_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithSelectedScheme());

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                ApplicationReferenceNumber = reference,
                FileUploadStatus = RegistrationTaskListStatus.Completed,
                PaymentViewStatus = RegistrationTaskListStatus.Completed,
                AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_UserHasNominatedNotification()
    {
        var notificationDtoList = new List<NotificationDto>
        {
            new()
            {
                Type = NotificationTypes.Packaging.DelegatedPersonNomination,
                Data = new List<KeyValuePair<string, string>>
                {
                    new("EnrolmentId", Guid.NewGuid().ToString())
                }
            }
        };

        // Arrange
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(GetSessionWithSelectedScheme());
        _notificationServiceMock.Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);
        
        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                Notification = new NotificationViewModel
                {
                    NominatedEnrolmentId = notificationDtoList.First().Data.ToList().FirstOrDefault().Value,
                    HasNominatedNotification = true,
                    HasPendingNotification = false,
                    NominatedApprovedPersonEnrolmentId = string.Empty
                },
                ApplicationStatus = ApplicationStatusType.NotStarted,
                ApplicationReferenceNumber = string.Empty
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_UserHasPendingApprovalNotification()
    {
        var notificationDtoList = new List<NotificationDto>
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

        // Arrange
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);
        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithSelectedScheme());
        _notificationServiceMock.Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _systemUnderTest.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeTwoId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                Notification = new NotificationViewModel
                {
                    NominatedEnrolmentId = string.Empty,
                    HasNominatedNotification = false,
                    HasPendingNotification = true,
                    NominatedApprovedPersonEnrolmentId = string.Empty
                },
                ApplicationStatus = ApplicationStatusType.NotStarted,
                ApplicationReferenceNumber = string.Empty
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(2));
    }

    [Test]
    public async Task Post_UpdatesSessionAndRedirectsToGet_WhenSelectedComplianceSchemeIdIsValid()
    {
        // Arrange
        var capturedSession = new FrontendSchemeRegistrationSession();
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock.Setup(x => x.GetOperatorComplianceSchemes(It.IsAny<Guid>())).ReturnsAsync(complianceSchemes);
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithoutSelectedScheme());
        _sessionManagerMock
            .Setup(x => x.UpdateSessionAsync(
                It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()))
            .Callback<ISession, Action<FrontendSchemeRegistrationSession>>((_, action) => action.Invoke(capturedSession));

        // Act
        var result = await _systemUnderTest.Post(_complianceSchemeOneId.ToString()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeLandingController.Get));
        capturedSession.RegistrationSession.SelectedComplianceScheme.Should().BeEquivalentTo(complianceSchemes[0]);
    }

    [Test]
    public async Task Post_DoesNotUpdateSessionAndRedirectsToGet_WhenSelectedComplianceSchemeIdIsUnknown()
    {
        // Arrange
        var complianceSchemes = GetComplianceSchemes();
        var unknownComplianceSchemeId = Guid.NewGuid().ToString();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithoutSelectedScheme());
        _complianceSchemeServiceMock.Setup(x => x.GetOperatorComplianceSchemes(It.IsAny<Guid>())).ReturnsAsync(complianceSchemes);

        // Act
        var result = await _systemUnderTest.Post(unknownComplianceSchemeId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeLandingController.Get));

        _sessionManagerMock.Verify(
            x => x.UpdateSessionAsync(
                It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()), Times.Never);
    }

    private List<ComplianceSchemeDto> GetComplianceSchemes() => new()
    {
        new ComplianceSchemeDto
        {
            Id = _complianceSchemeOneId,
            CreatedOn = DateTimeOffset.Now
        },
        new ComplianceSchemeDto
        {
            Id = _complianceSchemeTwoId,
            CreatedOn = DateTimeOffset.Now.AddDays(1)
        }
    };

    private FrontendSchemeRegistrationSession GetSessionWithSelectedScheme() =>
        new()
        {
            RegistrationSession = new()
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Id = _complianceSchemeTwoId
                },
            },
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = OrganisationName,
                        Id = _organisationId
                    }
                }
            }
        };

    private FrontendSchemeRegistrationSession GetSessionWithoutSelectedScheme() =>
        new()
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = OrganisationName,
                        Id = _organisationId
                    }
                }
            }
        };
}
