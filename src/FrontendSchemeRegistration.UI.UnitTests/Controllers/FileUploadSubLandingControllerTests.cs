using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class FileUploadSubLandingControllerTests
{
    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = $"Jan to Jun {DateTime.Now.Year}",
            Deadline = DateTime.Parse($"1/10/{DateTime.Now.Year} 11:59:00 PM"),
            Year = DateTime.Now.Year.ToString(),
            StartMonth = DateTime.Now.ToString("MMMM"),
            EndMonth = DateTime.Now.ToString("MMMM")
        },
        new SubmissionPeriod
        {
            DataPeriod = $"Jul to Dec {DateTime.Now.Year}",
            Deadline = DateTime.Parse($"1/04/{DateTime.Now.Year + 1} 11:59:00 PM"),
            Year = DateTime.Now.Year.ToString(),
            StartMonth = DateTime.Now.ToString("MMMM"),
            EndMonth = DateTime.Now.ToString("MMMM")
        }
    };

    private FileUploadSubLandingController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private Mock<ISession> _httpContextSessionMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
    private Mock<IResubmissionApplicationService> _resubmissionApplicationServicMock;

    [SetUp]
    public void SetUp()
    {
        _httpContextSessionMock = new Mock<ISession>();
        _submissionServiceMock = new Mock<ISubmissionService>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _resubmissionApplicationServicMock = new Mock<IResubmissionApplicationService>();

        _resubmissionApplicationServicMock.Setup(x => x.GetPackagingResubmissionApplicationSession(It.IsAny<Organisation>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
            .ReturnsAsync([new PackagingResubmissionApplicationSession { IsResubmissionFeeViewed = false, FileReachedSynapse = false, IsSubmitted = false }]);

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMSubmission2025))).ReturnsAsync(true);

        _systemUnderTest = new FileUploadSubLandingController(
            _submissionServiceMock.Object,
            _sessionMock.Object,
            _featureManagerMock.Object,
            Options.Create(new GlobalVariables { SubmissionPeriods = _submissionPeriods }),
            _resubmissionApplicationServicMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = _httpContextSessionMock.Object }
        };
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ReturnsCorrectViewModel_WhenCalled(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        var submissionPeriodDetailGroups = new List<SubmissionPeriodDetailGroup>
        {
            new()
            {
                DatePeriodYear = DateTime.Now.Year.ToString(),
                Quantity = 2,
                SubmissionPeriodDetails = new List<SubmissionPeriodDetail>
                {
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(0).DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    Status = SubmissionPeriodStatus.FileUploaded,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    Comments = string.Empty,
                    Decision = string.Empty,
                    IsResubmissionRequired = false,
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.NotStarted,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Comments = string.Empty,
                    Decision = string.Empty,
                    IsResubmissionRequired = false,
                    IsResubmissionComplete = false,
                }
                }
            }
        };

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeEquivalentTo(new FileUploadSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = organisationRole
        });
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ReturnsCorrectViewModel_WhenShowPoMSubmission2025_Is_False(string organisationRole)
    {
        // Arrange
        //_featureManagerMock.Reset();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMSubmission2025))).ReturnsAsync(false);

        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        var model = result.Model as FileUploadSubLandingViewModel;
        model.Should().NotBeNull();
        model.SubmissionPeriodDetailGroups.SelectMany(s => s.SubmissionPeriodDetails).Count(s => s.DatePeriodYear == "2025").Should().Be(0);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsCannotStartYetBeforeSubmissionPeriod_WhenCalled(string organisationRole)
    {
        // Arrange
        var submissionPeriods = new List<SubmissionPeriod>
        {
            new()
            {
                DataPeriod = $"Jan to Jun {DateTime.Now.Year + 1}",
                Deadline = DateTime.Parse($"1/10/{DateTime.Now.Year + 1} 11:59:00 PM"),
                ActiveFrom = DateTime.Parse($"1/10/{DateTime.Now.Year + 1} 11:59:00 PM"),
                Year = DateTime.Now.Year.ToString(),
                StartMonth = DateTime.Now.ToString("MMMM"),
                EndMonth = DateTime.Now.ToString("MMMM")
            },
            new()
            {
                DataPeriod = $"Jul to Dec {DateTime.Now.Year + 1}",
                Deadline = DateTime.Parse($"1/04/{DateTime.Now.Year + 2} 11:59:00 PM"),
                ActiveFrom = DateTime.Parse($"1/10/{DateTime.Now.Year + 1} 11:59:00 PM"),
                Year = DateTime.Now.Year.ToString(),
                StartMonth = DateTime.Now.ToString("MMMM"),
                EndMonth = DateTime.Now.ToString("MMMM")
            }
        };

        _systemUnderTest = new FileUploadSubLandingController(
            _submissionServiceMock.Object,
            _sessionMock.Object,
            _featureManagerMock.Object,
            Options.Create(new GlobalVariables { SubmissionPeriods = submissionPeriods }),
            _resubmissionApplicationServicMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = _httpContextSessionMock.Object }
        };
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails.
            Should().AllSatisfy(x => x.Status.Should().Be(SubmissionPeriodStatus.CannotStartYet));
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsNotStarted_WhenNoFileWasUploadedBefore(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission>());
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.NotStarted);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsSubmittedToRegulator_WhenUploadedFileWasSubmittedBefore(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = new()
            {
                FileName = "POM.csv",
                SubmittedDateTime = DateTime.Now.AddDays(-7),
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsFileUploaded_WhenUploadedFileHasNotBeenSubmittedBefore(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = null
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.FileUploaded);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsInProgress_WhenResubmissionIsInProgress(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = null,
        };
        var pomSubmission2 = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[1].DataPeriod,
            LastSubmittedFile = null,
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission, pomSubmission2 });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(true);

        _resubmissionApplicationServicMock.Setup(x => x.GetPackagingResubmissionApplicationSession(It.IsAny<Organisation>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<PackagingResubmissionApplicationSession> { new PackagingResubmissionApplicationSession { SubmissionId = submissionId, ApplicationStatus = ApplicationStatusType.FileUploaded, IsResubmissionFeeViewed = false, FileReachedSynapse = false, IsSubmitted = false } });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.InProgress);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsFileUploaded_WhenResubmissionIsInProgress_But_PackagingDataResubmissionJourney_NotImplemented(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = null,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.FileUploaded);
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[1].Status.Should().Be(SubmissionPeriodStatus.NotStarted);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer, null)]
    [TestCase(OrganisationRoles.Producer, false)]
    [TestCase(OrganisationRoles.ComplianceScheme, null)]
    [TestCase(OrganisationRoles.ComplianceScheme, false)]
    public async Task Get_ShowsFileUploaded_WhenResubmissionDoesNotHaveAValueOrIsNull_And_PackagingDataResubmissionJourney_IsImplemented(
        string organisationRole,
        bool? isResubmissionInProgress)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = null,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.FileUploaded);
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[1].Status.Should().Be(SubmissionPeriodStatus.NotStarted);
    }


    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsSubmittedToRegulator_WhenResubmissionIsCompleted_AndDecisionIsPendingWithRegulator(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = new SubmittedFileInformation() { FileId = new Guid() },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsSubmittedToRegulator_WhenNewFileUploadedAfterPreviousSubmissionToRegulator(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = new()
            {
                FileName = "POM.csv",
                SubmittedDateTime = DateTime.Now.AddDays(-7),
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
    }

    [Test]
    public async Task Get_ClearsSessionJourney_WhenCalled()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = selectedComplianceScheme,
                    Journey = new List<string> { PagePaths.FileUploadSubLanding, PagePaths.FileUpload }
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.ComplianceScheme
                        }
                    }
                }
            });

        // Act
        await _systemUnderTest.Get();

        // Assert
        _sessionMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(s => !s.RegistrationSession.Journey.Contains(PagePaths.FileUploadSubLanding)
                && !s.RegistrationSession.Journey.Contains(PagePaths.FileUpload))), Times.Once);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsSubmittedToRegulator_WhenUploadedFileWasSubmittedAfter(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            PomFileUploadDateTime = DateTime.Now.AddDays(-14),
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = new()
            {
                FileId = Guid.NewGuid(),
                FileName = "POM.csv",
                SubmittedDateTime = DateTime.Now.AddDays(-7),
            },
            LastUploadedValidFile = new()
            {
                FileId = Guid.NewGuid(),
                FileUploadDateTime = DateTime.Now.AddDays(-5),
            }

        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubLanding");
        result.Model.Should().BeOfType<FileUploadSubLandingViewModel>();
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetailGroups[0].SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
    }

    [Test]
    [TestCase("None", false, SubmissionPeriodStatus.SubmittedToRegulator)]
    [TestCase("Accepted", false, SubmissionPeriodStatus.AcceptedByRegulator)]
    [TestCase("Rejected", false, SubmissionPeriodStatus.RejectedByRegulator)]
    [TestCase("Approved", false, SubmissionPeriodStatus.AcceptedByRegulator)]
    [TestCase("None", true, SubmissionPeriodStatus.SubmittedToRegulator)]
    [TestCase("Accepted", true, SubmissionPeriodStatus.AcceptedByRegulator)]
    [TestCase("Rejected", true, SubmissionPeriodStatus.RejectedByRegulator)]
    [TestCase("Approved", true, SubmissionPeriodStatus.AcceptedByRegulator)]
    public async Task GetRegulatorDecision_ReturnsCorrectDecision_WhenCalled(string decisionValue, bool resubmit, SubmissionPeriodStatus submissionStatus)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var comment = "Test Comment";
        var organisationRole = OrganisationRoles.Producer;

        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = "Test_File.csv",
                SubmittedDateTime = DateTime.Now.AddMonths(-1),
                SubmittedBy = Guid.NewGuid()
            }
        };

        var pomDecision = new PomDecision
        {
            Comments = comment,
            Decision = decisionValue,
            IsResubmissionRequired = resubmit
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });

        _submissionServiceMock.Setup(x => x.GetDecisionAsync<PomDecision>(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<SubmissionType>()))
            .ReturnsAsync(pomDecision);

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission))).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                    .ReturnsAsync(new FrontendSchemeRegistrationSession
                    {
                        RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                        UserData = new UserData
                        {
                            Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                        }
                    });

        _resubmissionApplicationServicMock.Setup(x => x.GetPackagingResubmissionApplicationSession(It.IsAny<Organisation>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
                  .ReturnsAsync(new List<PackagingResubmissionApplicationSession> { new PackagingResubmissionApplicationSession { SubmissionId = submissionId } });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().NotBeNull();
        result.ViewName.Should().NotBeNull();
        result.ViewName.Should().Be("FileUploadSubLanding");

        var spds = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods[0].StartMonth,
                    DatePeriodEndMonth = _submissionPeriods[0].EndMonth,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    Deadline = _submissionPeriods[0].Deadline,
                    Status = submissionStatus,
                    Comments = comment,
                    Decision = decisionValue,
                    IsResubmissionRequired = resubmit,
                    IsResubmissionComplete = false
                },
                new()
                {
                    DataPeriod = _submissionPeriods[1].DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods[1].StartMonth,
                    DatePeriodEndMonth = _submissionPeriods[1].EndMonth,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    Deadline = _submissionPeriods[1].Deadline,
                    Status = SubmissionPeriodStatus.NotStarted,
                    Comments = string.Empty,
                    Decision = string.Empty,
                    IsResubmissionRequired = false
                }
            };

        var submissionPeriodDetailGroups = new List<SubmissionPeriodDetailGroup>
                     {
                         new()
                         {
                             DatePeriodYear = DateTime.Now.Year.ToString(),
                             Quantity = 2
                         }
                     };

        foreach (var group in submissionPeriodDetailGroups)
        {
            group.SubmissionPeriodDetails = spds.Where(c => c.DatePeriodYear == group.DatePeriodYear).ToList();
        }

        result.Model.Should().BeEquivalentTo(new FileUploadSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = organisationRole
        });
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    public async Task Get_ReturnsSubmissionPeriods_CorrectOrder_WhenCalled(string organisationRole)
    {
        var submissionPeriodsForMultipleYears = new List<SubmissionPeriod>
        {
            new SubmissionPeriod
            {
                DataPeriod = "Data period 1",
                Deadline = DateTime.Today,
                ActiveFrom = DateTime.Today,
                Year = "2023",
                StartMonth = "January",
                EndMonth = "June"
            },
            new SubmissionPeriod
            {
                DataPeriod = "Data period 2",
                Deadline = DateTime.Today.AddDays(5),
                ActiveFrom = DateTime.Today.AddDays(5),
                Year = "2024",
                StartMonth = "July",
                EndMonth = "December"
            }
        };

        _systemUnderTest = new FileUploadSubLandingController(
        _submissionServiceMock.Object,
        _sessionMock.Object,
        _featureManagerMock.Object,
        Options.Create(new GlobalVariables { SubmissionPeriods = submissionPeriodsForMultipleYears }),
        _resubmissionApplicationServicMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = _httpContextSessionMock.Object }
        };
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = organisationRole } }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        var viewModel = result.Model as FileUploadSubLandingViewModel;
        viewModel.Should().NotBeNull();

        var submissionPeriodYear1 = int.Parse(viewModel.SubmissionPeriodDetailGroups[0].DatePeriodYear);
        var submissionPeriodYear2 = int.Parse(viewModel.SubmissionPeriodDetailGroups[1].DatePeriodYear);

        submissionPeriodYear1.Should().BeGreaterThanOrEqualTo(submissionPeriodYear2);
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    public async Task Get_ReturnsSubmissionPeriods_RedirectsTo_Regitration_LandingPage_WhenCalled(string organisationRole)
    {
        var submissionPeriodsForMultipleYears = new List<SubmissionPeriod>
        {
            new SubmissionPeriod
            {
                DataPeriod = "Data period 1",
                Deadline = DateTime.Today,
                ActiveFrom = DateTime.Today,
                Year = "2023",
                StartMonth = "January",
                EndMonth = "June"
            },
            new SubmissionPeriod
            {
                DataPeriod = "Data period 2",
                Deadline = DateTime.Today.AddDays(5),
                ActiveFrom = DateTime.Today.AddDays(5),
                Year = "2024",
                StartMonth = "July",
                EndMonth = "December"
            }
        };

        _systemUnderTest = new FileUploadSubLandingController(
        _submissionServiceMock.Object,
        _sessionMock.Object,
        _featureManagerMock.Object,
        Options.Create(new GlobalVariables { SubmissionPeriods = submissionPeriodsForMultipleYears }),
        _resubmissionApplicationServicMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = _httpContextSessionMock.Object }
        };
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        var submissionId = Guid.NewGuid();
        var pomSubmission = new PomSubmission
        {
            Id = submissionId,
            HasValidFile = true,
            SubmissionPeriod = _submissionPeriods[0].DataPeriod
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<PomSubmission> { pomSubmission });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .ReturnsAsync(pomSubmission);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { SelectedComplianceScheme = selectedComplianceScheme },
                UserData = new UserData { Organisations = new List<Organisation> { new Organisation { Id = Guid.NewGuid() } } }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.LandingPage));
    }

    [Test]
    public async Task Post_RedirectsToGetAction_IfSubmissionPeriodFromPayloadIsInvalid()
    {
        // Arrange
        const string submissionPeriod = "invalid";

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadSubLandingController.Get));
    }

    [Test]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToFileUploadWithSubmissionIdQueryParam_WhenSubmissionExistsAndIsNotSubmitted()
    {
        // Arrange
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadController).RemoveControllerFromName());
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);

        _sessionMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    s => s.RegistrationSession.SubmissionPeriod == submissionPeriod && s.RegistrationSession.SubmissionDeadline == submissionDeadline)),
            Times.AtMost(2));
    }

    [Test]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToFileUploadWithNoSubmissionIdQueryParam_WhenSubmissionDoesNotExist()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission>());
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadController).RemoveControllerFromName());
        result.RouteValues.Should().BeNull();

        _sessionMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    s => s.RegistrationSession.SubmissionPeriod == _submissionPeriods[0].DataPeriod)), Times.Once);
    }

    [Test]
    public async Task Post_PopulatesJourneyInSessionAndRedirectsToFileUpload_WhenSubmissionDoesNotExist()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission>());
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new()
                {
                    Journey = new List<string> { PagePaths.FileUploadSubLanding }
                },
            });

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be(nameof(FileUploadController.Get));
        result?.ControllerName.Should().Be(nameof(FileUploadController).RemoveControllerFromName());
        result?.RouteValues.Should().BeNull();

        _sessionMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    s => s.RegistrationSession.Journey.Contains(PagePaths.FileUploadSubLanding)
                         && !s.RegistrationSession.Journey.Contains(PagePaths.FileUpload))), Times.Once);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCheckFileAndSubmitGetWithSubmissionIdQueryParam_WhenSubmissionIsNotSubmittedButHasValidFile()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            HasValidFile = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = fileId
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCheckFileAndSubmitController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName());
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_WhenSubmissionHasWarningsAndValidationPassed_RedirectsToFileUploadWarning()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            HasWarnings = true,
            ValidationPass = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = fileId
            }
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<RedirectToActionResult>();
        result.ControllerName.Should().Be(nameof(FileUploadWarningController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(FileUploadWarningController.Get));
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToUploadNewFileToSubmitGetWithSubmissionIdQueryParam_WhenLastUploadedFileIdIsSameAsLastSubmittedFileId()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = fileId
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = fileId
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(UploadNewFileToSubmitController.Get));
        result.ControllerName.Should().Be(nameof(UploadNewFileToSubmitController).RemoveControllerFromName());
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCheckFileAndSubmitGetWithSubmissionIdQueryParam_WhenLastUploadedFileIdIsNotTheSameAsLastSubmittedFileId()
    {
        // Arrange
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCheckFileAndSubmitController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsTo_FileUploadCheckFileAndSubmit_When_ImplementResubmissionJourney_IsFalse()
    {
        // Arrange
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCheckFileAndSubmitController.Get));
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsTo_UploadNewFileToSubmit_When_ImplementResubmissionJourney_IsTrue()
    {
        // Arrange
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(UploadNewFileToSubmitController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsTo_UploadNewFileToSubmit_When_ImplementResubmissionJourney_IsFalse()
    {
        // Arrange
        var submissionDeadline = _submissionPeriods[0].Deadline;
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(UploadNewFileToSubmitController.Get));
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_WhenSubmissionHasWarningsValidationPassedAndFileIdsDiffer_RedirectsToFileUploadWarning()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney))).ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadWarningController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadWarning_When_SubmissionHasWarningsValidationPassedAndFileIdsDiffer_And_ImplementResubmissionJourney_IsFalse()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadWarningController.Get));
        result.ControllerName.Should().Be("FileUploadWarning");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToUploadNewFileToSubmit_When_Not_SubmissionHasWarningsValidationPassed_And_FileIdsSame_And_ImplementResubmissionJourney_IsFalse()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.LastSubmittedFile.FileId = submission.LastUploadedValidFile.FileId;
        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadWarningController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCheckFileAndSubmit_When_Not_SubmissionHasWarningsValidationPassed_And_FileIdsDiffer_And_ImplementResubmissionJourney_IsFalse()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadWarningController.Get));
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubLanding_When_OrganisationId_IsNull()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadSubLandingController.Get));
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUpload_When_InitialSubmissionWasNeverAccepted()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(false);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
        result.ControllerName.Should().Be("FileUpload");
    }

    [Test]
    public async Task Post_RedirectsToResubmissionTaskList_When_ResubmissionIsInProgress()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.FileUploaded
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(PackagingDataResubmissionController.ResubmissionTaskList));
        result.ControllerName.Should().Be("PackagingDataResubmission");
    }

    [Test]
    public async Task Post_RedirectsToResubmissionTaskList_When_ResubmissionIsComplete()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                            FileReachedSynapse = true,
                            ResubmissionFeePaymentMethod = "PayOnline",
                            ResubmissionApplicationSubmittedDate = DateTime.UtcNow,
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(PackagingDataResubmissionController.ResubmissionTaskList));
        result.ControllerName.Should().Be("PackagingDataResubmission");
    }

    [Test]
    public async Task Post_RedirectsToResubmissionTaskList_When_ResubmissionIsNotInProgress_And_ResubmissionIsNotComplete()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(UploadNewFileToSubmitController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
    }

    [Test]
    public async Task Post_RedirectsToFileUpload_When_ComplianceSchemeId_IsNull()
    {
        // Arrange
        var submissionPeriod = _submissionPeriods[0].DataPeriod;
        var submission = CreatePomSubmissionWithWarningsAndFileIdMismatch();

        submission.HasWarnings = false;
        submission.ValidationPass = false;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });

        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), null)).ReturnsAsync(true);

        _sessionMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = submissionPeriod
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation> { new() { Id = Guid.NewGuid(), OrganisationRole = OrganisationRoles.Producer } }
                },
                PomResubmissionSession = new PackagingReSubmissionSession()
                {
                    PackagingResubmissionApplicationSessions = new List<PackagingResubmissionApplicationSession>()
                    {
                        new PackagingResubmissionApplicationSession()
                        {
                            SubmissionId = submission.Id,
                            ApplicationStatus = ApplicationStatusType.NotStarted
                        }
                    }
                }
            });

        _featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(UploadNewFileToSubmitController.Get));
        result.ControllerName.Should().Be("UploadNewFileToSubmit");
    }

    private static PomSubmission CreatePomSubmissionWithWarningsAndFileIdMismatch()
    {
        return new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            HasWarnings = true,
            ValidationPass = true,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            }
        };
    }
}