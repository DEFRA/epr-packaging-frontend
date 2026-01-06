using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.Notification;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Enums;
using Microsoft.Extensions.Time.Testing;
using DateTimeOffset = System.DateTimeOffset;

[TestFixture]
public class ComplianceSchemeLandingControllerTests
{
    private const string OrganisationName = "Acme Org Ltd";

    private readonly string _currentYear = DateTime.Now.Year.ToString();
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly Guid _complianceSchemeOneId = Guid.NewGuid();
    private readonly Guid _complianceSchemeTwoId = Guid.NewGuid();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly NullLogger<ComplianceSchemeLandingController> _nullLogger = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IComplianceSchemeService> _complianceSchemeServiceMock = new();
    private readonly Mock<IRegistrationApplicationService> _registrationApplicationService = new();
    private readonly Mock<ISubmissionService> _submissionService = new();
    private readonly Mock<IResubmissionApplicationService> _resubmissionApplicationService = new();
    protected Mock<IOptions<GlobalVariables>> globalVariables { get; set; }

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock = new();
    private ComplianceSchemeLandingController _complianceSchemeLandingController;
    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = "Data period 1",
            Deadline = DateTime.Parse("2023-03-31"),
            ActiveFrom = DateTime.Parse("2023-01-01"),
            Year = "2023",
            StartMonth = "September",
            EndMonth = "December",
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 2",
            Deadline = DateTime.Parse("2024-03-31"),
            ActiveFrom = DateTime.Parse("2024-01-01"),
            Year = "2024",
            StartMonth = "January",
            EndMonth = "March"
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 3",
            Deadline = DateTime.Parse("2025-03-31"),
            ActiveFrom = DateTime.Parse("2025-01-01"),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 4",
            Deadline = DateTime.Parse("2026-03-31"),
            ActiveFrom = DateTime.Parse("2026-01-01"),
            Year = "2026",
            StartMonth = "January",
            EndMonth = "June"
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 5",
            Deadline = DateTime.Parse("2027-03-31"),
            ActiveFrom = DateTime.Parse("2027-01-01"),
            Year = "2027",
            StartMonth = "January",
            EndMonth = "June"
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 6",
            Deadline = DateTime.Parse("2028-03-31"),
            ActiveFrom = DateTime.Parse("2028-01-01"),
            Year = "2028",
            StartMonth = "January",
            EndMonth = "June"
        }
    };

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

    private int _overrideCurrentYear = 2026;
    private TimeProvider _testTimeProvider;
    private int _overrideCurrentMonth = 1;

    [SetUp]
    public void SetUp()
    {
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations =
            [
                new Organisation
                {
                    Id = _organisationId,
                    Name = OrganisationName,
                    OrganisationRole = "ComplianceScheme",
                    OrganisationNumber = "552555"
                }
            ],
            ServiceRole = "Approved Person"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData))
        };

        globalVariables = new Mock<IOptions<GlobalVariables>>();
        
        globalVariables.Setup(o => o.Value)
            .Returns(new GlobalVariables { 
                BasePath = "path", 
                SubmissionPeriods = _submissionPeriods,
                OverrideCurrentMonth = _overrideCurrentMonth,
                OverrideCurrentYear = _overrideCurrentYear });

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _notificationServiceMock.Setup(x => x.GetCurrentUserNotifications(It.IsAny<Guid>(), It.IsAny<Guid>()));
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

        _resubmissionApplicationService.Setup(x => x.GetCurrentMonthAndYearForRecyclingObligations(_testTimeProvider))
            .Returns(Task.FromResult((1, _overrideCurrentYear)));
        _resubmissionApplicationService.Setup(x => x.PackagingResubmissionPeriod(It.IsAny<string[]>(), It.IsAny<DateTime>()))
            .Returns(_submissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear));

        //can be used for time travel testing
        _testTimeProvider = TimeProvider.System;
        
        _complianceSchemeLandingController = new ComplianceSchemeLandingController(
            _sessionManagerMock.Object,
            _complianceSchemeServiceMock.Object,
            _notificationServiceMock.Object,
            _registrationApplicationService.Object,
            _resubmissionApplicationService.Object,
            _nullLogger,
            _testTimeProvider)
        {
            ControllerContext = { HttpContext = _httpContextMock.Object }
        };
    }

    [Test]
    [TestCase(2025, 11, 2026, 1, 2025)]
    [TestCase(2025, 11, 2026, 4, 2025)]
    [TestCase(2025, 11, 2027, 1, 2025)]
    
    [TestCase(2025, 12, 2026, 1, 2025)]
    [TestCase(2025, 12, 2026, 5, 2025)]
    [TestCase(2025, 12, 2027, 1, 2025)]
    
    [TestCase(2026,  1, 2026, 1, 2025)]
    [TestCase(2026,  1, 2026, 6, 2025)]
    [TestCase(2026,  1, 2027, 1, 2025)]
    
    [TestCase(2026,  2, 2026, 1, 2026)]
    [TestCase(2026,  2, 2027, 1, 2026)]
    
    [TestCase(2026,  3, 2026, 1, 2026)]
    [TestCase(2026,  3, 2027, 1, 2026)]
    
    [TestCase(2026,  6, 2026, 1, 2026)]
    [TestCase(2026,  6, 2027, 1, 2026)]
    
    [TestCase(2027,  1, 2026, 1, 2026)]
    [TestCase(2027,  1, 2027, 1, 2026)]
    
    [TestCase(2026,  6, null, null, 2026)]
    [TestCase(2027,  1, null, null, 2026)]
    
    public async Task Get_ReturnsCorrectViewAndModel_WhenSelectedComplianceSchemeDoesNotExistInSession(
        int year, int month, int? overrideCurrentYearConfigSetting, int? overrideCurrentMonthConfigSetting,
        int expectedComplianceYear)
    {
        //override Setup
        globalVariables.Setup(o => o.Value)
            .Returns(new GlobalVariables { 
                BasePath = "path", 
                SubmissionPeriods = _submissionPeriods,
                OverrideCurrentMonth = overrideCurrentMonthConfigSetting,
                OverrideCurrentYear = overrideCurrentYearConfigSetting });
        
        //Time travel setup
        var ftp = new FakeTimeProvider();
        ftp.SetUtcNow(new DateTimeOffset(new DateTime(year, month, 01)));
        _complianceSchemeLandingController.SetTestTimeProvider(ftp);
        
        _resubmissionApplicationService.Setup(x => x.GetCurrentMonthAndYearForRecyclingObligations(ftp))
            .Returns(Task.FromResult((month, year)));
        
        _resubmissionApplicationService.Setup(x => x.PackagingResubmissionPeriod(It.IsAny<string[]>(), It.Is<DateTime>(dt => dt.Month != 1)))
            .Returns(_submissionPeriods.FirstOrDefault(sp => sp.Year == year.ToString() && sp.ActiveFrom.Year == year));
        _resubmissionApplicationService.Setup(x => x.PackagingResubmissionPeriod(It.IsAny<string[]>(), It.Is<DateTime>(dt => dt.Month == 1)))
            .Returns(_submissionPeriods.FirstOrDefault(sp => sp.Year == (year-1).ToString() && sp.ActiveFrom.Year == year-1));
        //End Time travel Setup
        
        var complianceSchemes = GetComplianceSchemes();
        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(GetSessionWithoutSelectedScheme());

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);
        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = _registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = _registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = _registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = _registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = _registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = _registrationApplicationSession.IsResubmission,
           }
        };

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>()
                {
                    new RegistrationApplicationPerYearViewModel
                    {
                        ApplicationStatus = ApplicationStatusType.NotStarted,
                        ApplicationReferenceNumber = string.Empty,
                        RegistrationReferenceNumber = string.Empty,
                    }
                },
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == expectedComplianceYear.ToString()),
                ComplianceYear = expectedComplianceYear.ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
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

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = _registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = _registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = _registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = _registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = _registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = _registrationApplicationSession.IsResubmission
            }
        };

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>()
                {
                    new RegistrationApplicationPerYearViewModel
                    {
                        ApplicationStatus = ApplicationStatusType.NotStarted,
                        ApplicationReferenceNumber = string.Empty,
                        RegistrationReferenceNumber = string.Empty,
                    }
                },
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
        
        _resubmissionApplicationService.Verify(
            x => x.GetCurrentMonthAndYearForRecyclingObligations(
                It.IsAny<TimeProvider>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_FileUploaded_Is_PendingState_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        var session = GetSessionWithSelectedScheme();

        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

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

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };
        
        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                RegistrationApplicationsPerYear =
                [
                    new RegistrationApplicationPerYearViewModel
                    {
                        ApplicationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Pending,
                        PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                        ApplicationStatus = ApplicationStatusType.FileUploaded,
                    }
                ],
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        var session = new FrontendSchemeRegistrationSession();

        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            RegistrationReferenceNumber = null,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

        // Assert
        response.ViewName.Should().Be("ComplianceSchemeLanding");
        response.Model.Should()
            .BeOfType<ComplianceSchemeLandingViewModel>()
            .And
            .BeEquivalentTo(new ComplianceSchemeLandingViewModel
            {
                CurrentComplianceSchemeId = _complianceSchemeOneId,
                OrganisationName = OrganisationName,
                CurrentTabSummary = new ComplianceSchemeSummary(),
                ComplianceSchemes = complianceSchemes,
                IsApprovedUser = true,
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>()
                {
                    new RegistrationApplicationPerYearViewModel{

                        ApplicationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Completed,
                        PaymentViewStatus = RegistrationTaskListStatus.Completed,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
                        ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,}
                },
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_When_ApplicationSubmittedToRegulator_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();
        var complianceSchemes = GetComplianceSchemes();
        var session = GetSessionWithSelectedScheme();

        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            RegistrationReferenceNumber = null,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(registrationApplicationSession);

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = registrationApplicationSession.IsResubmission
           }
        };

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>()
                {
                    new RegistrationApplicationPerYearViewModel{
                        ApplicationReferenceNumber = reference,
                        FileUploadStatus = RegistrationTaskListStatus.Completed,
                        PaymentViewStatus = RegistrationTaskListStatus.Completed,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
                        ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                    }
                },
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_UserHasNominatedNotification()
    {
        _resubmissionApplicationService.Setup(x => x.GetCurrentMonthAndYearForRecyclingObligations(_testTimeProvider))
            .Returns(Task.FromResult((_testTimeProvider.GetUtcNow().Month, _testTimeProvider.GetUtcNow().Year)));
        
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

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = _registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = _registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = _registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = _registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = _registrationApplicationSession.RegistrationReferenceNumber,
            IsResubmission = _registrationApplicationSession.IsResubmission
           }
        };

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        var registrationApplicationDetails = (RegistrationApplicationDetails) null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>()
                {
                    new RegistrationApplicationPerYearViewModel
                    {
                        ApplicationStatus = ApplicationStatusType.NotStarted,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                        ApplicationReferenceNumber = string.Empty,
                        RegistrationReferenceNumber = string.Empty
                    }
                },

                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
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
        var session = GetSessionWithSelectedScheme();

        _complianceSchemeServiceMock
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync(complianceSchemes);

        _complianceSchemeServiceMock
            .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ComplianceSchemeSummary());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

        _notificationServiceMock.Setup(x => x.GetCurrentUserNotifications(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(notificationDtoList);

        _registrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>()))
            .ReturnsAsync(_registrationApplicationSession);

        var registrationApplicationPerYear = new List<RegistrationApplicationPerYearViewModel>()
        {
            new RegistrationApplicationPerYearViewModel {
            ApplicationStatus = _registrationApplicationSession.ApplicationStatus,
            FileUploadStatus = _registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = _registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = _registrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = _registrationApplicationSession.ApplicationReferenceNumber,
            IsResubmission = _registrationApplicationSession.IsResubmission
           }
        };

        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear);

        var registrationApplicationDetails = (RegistrationApplicationDetails)null;
        _submissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var response = await _complianceSchemeLandingController.Get() as ViewResult;

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
                RegistrationApplicationsPerYear = new List<RegistrationApplicationPerYearViewModel>
                {
                    new RegistrationApplicationPerYearViewModel
                    {
                        ApplicationStatus = ApplicationStatusType.NotStarted,
                        ApplicationReferenceNumber = string.Empty,
                        AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
                        RegistrationReferenceNumber = string.Empty
                    }
                },
                ResubmissionTaskListViewModel = new ResubmissionTaskListViewModel
                {
                    AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ApplicationStatus = ApplicationStatusType.NotStarted,
                    FileReachedSynapse = false,
                    FileUploadStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    IsComplianceScheme = false,
                    IsSubmitted = false,
                    OrganisationName = string.Empty,
                    OrganisationNumber = string.Empty,
                    PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
                    ResubmissionApplicationSubmitted = false
                },
                PackagingResubmissionPeriod = globalVariables.Object.Value.SubmissionPeriods.FirstOrDefault(sp => sp.Year == _currentYear),
                ComplianceYear = (_overrideCurrentYear-1).ToString()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Exactly(1));
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
        var result = await _complianceSchemeLandingController.Post(_complianceSchemeOneId.ToString()) as RedirectToActionResult;

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
        var result = await _complianceSchemeLandingController.Post(unknownComplianceSchemeId) as RedirectToActionResult;

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
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Id = _complianceSchemeTwoId
                }
            },
            UserData = new UserData
            {
                Organisations =
                [
                    new Organisation
                    {
                        Name = OrganisationName,
                        Id = _organisationId
                    }
                ]
            }
        };

    private FrontendSchemeRegistrationSession GetSessionWithoutSelectedScheme() =>
        new()
        {
            UserData = new UserData
            {
                Organisations =
                [
                    new Organisation
                    {
                        Name = OrganisationName,
                        Id = _organisationId
                    }
                ]
            }
        };
}