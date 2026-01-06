namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Extensions;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class PackagingDataResubmissionControllerTests : PackagingDataResubmissionTestBase
{
    private const string OrganisationName = "Acme Org Ltd";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private readonly Guid _organisationId = Guid.NewGuid();

    private UserData _userData;

    [SetUp]
    public void SetUp()
    {
        _userData = GetUserData("Producer");

        SetupBase(_userData);
    }

    private UserData GetUserData(string organisationRole)
    {
        return new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    Id = _organisationId,
                    OrganisationRole = organisationRole
                }
            }
        };
    }

    [Test]
    public async Task ResubmissionTaskList_CreatePomResubmissionReferenceNumberForProducer_ShouldNotBeCalled_WhenAppReferenceNumberExists()
    {
        // Arrange
        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission>
                    {
                        new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") }
                    },
                PomSubmission = new PomSubmission()
                {
                    Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
                    LastSubmittedFile = new SubmittedFileInformation
                    {
                        FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
                        SubmittedDateTime = DateTime.Now.AddDays(-2)
                    }
                },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    ApplicationReferenceNumber = "Test-Ref"
                }
            },
        };

        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(session));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
            It.IsAny<Organisation>(),
            It.IsAny<List<string>>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(resubmissionApplicationDetailsCollection);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("England");

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);

        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        ResubmissionApplicationService.Verify(x => x.CreatePomResubmissionReferenceNumberForProducer(It.IsAny<FrontendSchemeRegistrationSession?>(), It.IsAny<SubmissionPeriod>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ResubmissionTaskList_CreatePomResubmissionReferenceNumberForProducer_ShouldBeCalled_WhenAppReferenceNumberDoesnotExists()
    {
        // Arrange
        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        _userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    Id = _organisationId,
                    OrganisationRole = OrganisationRoles.ComplianceScheme
                }
            }
        };

        SetupBase(_userData);

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                SubmissionPeriod = "January to December 2024",
                PomSubmissions = new List<PomSubmission>
                        { new PomSubmission
                            { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") }
                        },
                PomSubmission = new PomSubmission()
                {
                    Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
                    IsSubmitted = true,
                    LastSubmittedFile = new SubmittedFileInformation
                    {
                        FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
                        SubmittedDateTime = DateTime.Now.AddDays(-2)
                    }
                }
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto
                {
                    Id = Guid.NewGuid()
                }
            }
        };
        var complianceSchemeSummary = new ComplianceSchemeSummary
        {
            Nation = Nation.England
        };

        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(session));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<Organisation>(), It.IsAny<List<string>>(), It.IsAny<Guid?>())).ReturnsAsync(resubmissionApplicationDetailsCollection);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("England");
        ComplianceService.Setup(x => x.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(complianceSchemeSummary));

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);
        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        ResubmissionApplicationService.Verify(x => x.CreatePomResubmissionReferenceNumber(It.IsAny<FrontendSchemeRegistrationSession?>(), It.IsAny<string?>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task ResubmissionTaskList_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession { PomResubmissionSession = new PackagingReSubmissionSession { PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }, PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } } } }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<Organisation>(), It.IsAny<List<string>>(), It.IsAny<Guid?>())).ReturnsAsync(resubmissionApplicationDetailsCollection);
        ResubmissionApplicationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("England");
        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("England");
        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);
        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Model.Should().BeOfType<ResubmissionTaskListViewModel>();

        result.Model.As<ResubmissionTaskListViewModel>().Should().BeEquivalentTo(new ResubmissionTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = ResubmissionTaskListStatus.NotStarted,
            PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
            FileReachedSynapse = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            IsResubmissionInProgress = false
        });
    }

    [Test]
    public async Task ResubmissionTaskList_IsSubmitted_ReturnsCorrectViewAndModel()
    {
        // Arrange
        FrontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } },
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } } },
                SubmissionPeriod = "January to December 2024"
            }
        };

        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();


        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);

        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontendSchemeRegistrationSession);

        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });

        var details = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var detailsCollection = new List<PackagingResubmissionApplicationDetails> { details };


        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
            It.IsAny<Organisation>(),
            It.IsAny<List<string>>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(detailsCollection);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Model.Should().BeOfType<ResubmissionTaskListViewModel>();

        result.Model.As<ResubmissionTaskListViewModel>().Should().BeEquivalentTo(new ResubmissionTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = ResubmissionTaskListStatus.NotStarted,
            PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
            FileReachedSynapse = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            IsResubmissionInProgress = false
        });
    }

    [Test]
    public async Task ResubmissionTaskList_IsSubmitted_ApplicationReferenceNumberIsNull_ReturnsCorrectViewAndModel()
    {
        // Arrange
        FrontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } },
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } } },
                SubmissionPeriod = "January to December 2024",
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession { ApplicationReferenceNumber = null }
            }
        };

        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontendSchemeRegistrationSession);

        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);

        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        var details = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var detailsCollection = new List<PackagingResubmissionApplicationDetails> { details };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
            It.IsAny<Organisation>(),
            It.IsAny<List<string>>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(detailsCollection);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Model.Should().BeOfType<ResubmissionTaskListViewModel>();

        result.Model.As<ResubmissionTaskListViewModel>().Should().BeEquivalentTo(new ResubmissionTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = ResubmissionTaskListStatus.NotStarted,
            PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
            FileReachedSynapse = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            IsResubmissionInProgress = false
        });
    }

    [Test]
    public async Task ResubmissionTaskList_IsSubmitted_SameDatePeriod_ReturnsCorrectViewAndModel()
    {
        // Arrange
        FrontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } } },
                PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } },
                SubmissionPeriod = "January to December 2024",
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    ApplicationReferenceNumber = ""
                }
            }
        };
        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontendSchemeRegistrationSession);

        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);

        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        var details = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var detailsCollection = new List<PackagingResubmissionApplicationDetails> { details };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
            It.IsAny<Organisation>(),
            It.IsAny<List<string>>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(detailsCollection);

        // Act
        var result = await SystemUnderTest.ResubmissionTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Model.Should().BeOfType<ResubmissionTaskListViewModel>();

        result.Model.As<ResubmissionTaskListViewModel>().Should().BeEquivalentTo(new ResubmissionTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = ResubmissionTaskListStatus.NotStarted,
            PaymentViewStatus = ResubmissionTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = ResubmissionTaskListStatus.CanNotStartYet,
            FileReachedSynapse = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            IsResubmissionInProgress = false,
        });
    }

    [Test]
    public void RedirectToFileUpload_ReturnsCorrectView()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = [PagePaths.FileUploadSubLanding],
                SubmissionPeriod = "April 2025 to March 2026",

            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission>()
            }
        };

        session.PomResubmissionSession.PomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation() { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now.AddDays(-2) },
            LastUploadedValidFile = new UploadedFileInformation() { FileId = Guid.NewGuid() },
            HasWarnings = false,
            ValidationPass = true,

        };


        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = SystemUnderTest.RedirectToFileUpload().Result as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
    }

    [Test]
    public void RedirectToFileUpload_Redirects_FileUpload()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = [PagePaths.FileUploadSubLanding],
                SubmissionPeriod = "April 2025 to March 2026",

            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission>()
            }
        };

        session.PomResubmissionSession.PomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation() { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") },
            LastUploadedValidFile = new UploadedFileInformation() { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") },
            HasWarnings = false,
            ValidationPass = true,
        };


        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = SystemUnderTest.RedirectToFileUpload().Result as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be("FileUpload");
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
    }

    [Test]
    public void RedirectToFileUpload_Redirects_To_FileUploadWarning_FileUpload()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = [PagePaths.FileUploadSubLanding],
                SubmissionPeriod = "April 2025 to March 2026",

            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission>()
            }
        };

        session.PomResubmissionSession.PomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation() { FileId = Guid.NewGuid() },
            LastUploadedValidFile = new UploadedFileInformation() { FileId = Guid.NewGuid() },
            HasWarnings = true,
            ValidationPass = true,
        };


        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = SystemUnderTest.RedirectToFileUpload().Result as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be("FileUploadWarning");
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
    }

    [Test]
    public async Task ResubmissionFeeCalculation_ReturnsCorrectViewAndModel()
    {
        // Arrange
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                    {
                        ApplicationReferenceNumber = "abc"
                    },
                    PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } },
                    PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }
                }
            }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        ResubmissionApplicationService.Setup(x => x.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime?>())).ReturnsAsync(new PackagingPaymentResponse());
        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ReturnsAsync(new PackagingResubmissionMemberDetails() { MemberCount = 1 });

        // Act
        var result = await SystemUnderTest.ResubmissionFeeCalculations() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Model.Should().BeOfType<ResubmissionFeeViewModel>();

        result.Model.As<ResubmissionFeeViewModel>().Should().BeEquivalentTo(new ResubmissionFeeViewModel
        {
            IsComplianceScheme = false,
            MemberCount = 1,
            PreviousPaymentsReceived = 0,
            ResubmissionFee = 0,
            TotalChargeableItems = 0,
            TotalOutstanding = 0
        });
    }

    [Test]
    public async Task ResubmissionFeeCalculation_When_FileIdIsNull_ReturnsCorrectViewAndModel()
    {
        // Arrange
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                    {
                        ApplicationReferenceNumber = "abc"
                    },
                    PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") },
                    PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }
                }
            }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        ResubmissionApplicationService.Setup(x => x.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime?>())).ReturnsAsync(new PackagingPaymentResponse());
        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ReturnsAsync(new PackagingResubmissionMemberDetails() { MemberCount = 1 });
        // Act
        var result = await SystemUnderTest.ResubmissionFeeCalculations() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Model.Should().BeOfType<ResubmissionFeeViewModel>();

        result.Model.As<ResubmissionFeeViewModel>().Should().BeEquivalentTo(new ResubmissionFeeViewModel
        {
            IsComplianceScheme = false,
            MemberCount = 1,
            PreviousPaymentsReceived = 0,
            ResubmissionFee = 0,
            TotalChargeableItems = 0,
            TotalOutstanding = 0
        });
    }

    [Test]
    public async Task ResubmissionFeeCalculation_HttpRequestExceptionEncountered_And_PreconditionRequired_StatuCode_ReturnModelStateError()
    {
        // Arrange
        var user = GetUserData("Compliance Scheme");
        SetupBase(user);

        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                    {
                        ApplicationReferenceNumber = "abc"
                    },
                    PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } },
                    PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }
                }
            }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        _userData.Organisations[0].OrganisationRole = OrganisationRoles.ComplianceScheme;

        ResubmissionApplicationService.Setup(x => x.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime>())).ReturnsAsync(new PackagingPaymentResponse());
        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ThrowsAsync(new HttpRequestException("message", null, System.Net.HttpStatusCode.PreconditionRequired));

        // Act
        var result = await SystemUnderTest.ResubmissionFeeCalculations() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Model.Should().BeOfType<ResubmissionFeeViewModel>();

        result.Model.As<ResubmissionFeeViewModel>().Should().BeEquivalentTo(new ResubmissionFeeViewModel());
        result.ViewData.ModelState.ErrorCount.Should().Be(1);
    }

    [Test]
    public async Task ResubmissionFeeCalculation_HttpRequestExceptionEncountered_And_Not_PreconditionRequired_StatuCode_ReturnNoModelStateError()
    {
        // Arrange
        var user = GetUserData("Compliance Scheme");
        SetupBase(user);

        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                    {
                        ApplicationReferenceNumber = "abc"
                    },
                    PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } },
                    PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }
                }
            }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });
        _userData.Organisations[0].OrganisationRole = OrganisationRoles.ComplianceScheme;

        ResubmissionApplicationService.Setup(x => x.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime?>())).ReturnsAsync(new PackagingPaymentResponse());
        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ThrowsAsync(new HttpRequestException("message", null, System.Net.HttpStatusCode.NotFound));

        // Act
        var result = await SystemUnderTest.ResubmissionFeeCalculations() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Model.Should().BeOfType<ResubmissionFeeViewModel>();

        result.Model.As<ResubmissionFeeViewModel>().Should().BeOfType(typeof(ResubmissionFeeViewModel));
        result.ViewData.ModelState.ErrorCount.Should().Be(0);
    }

    [Test]
    public async Task ResubmissionFeeCalculation_ReturnsCorrectAction()
    {
        // Arrange
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                    {
                        ApplicationReferenceNumber = "abc"
                    },
                    PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = DateTime.Now.AddDays(-2) } },
                    PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } }
                }
            }));
        UserAccountService.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });

        // Act
        var result = await SystemUnderTest.ResubmissionFeeCalculations() as RedirectToActionResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result?.ActionName.Should().Be(nameof(PackagingDataResubmissionController.ResubmissionTaskList));
    }


    [Test]
    public async Task GetMemberCount_ReturnsOneForProducers_WhenFeatureFlagIsOff()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var iscomplianceScheme = false;

        // Act
        var result = await SystemUnderTest.GetMemberCount(submissionId, iscomplianceScheme, It.IsAny<Guid?>());

        // Assert
        result.Should().Be(1);
    }

    [Test]
    public async Task GetMemberCount_ReturnsMemberCountForProducers_WhenFeatureFlagIsOn()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var memberCount = 2;
        var iscomplianceScheme = false;

        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ReturnsAsync(new PackagingResubmissionMemberDetails() { MemberCount = memberCount });
        ResubmissionApplicationService.Setup(x => x.GetFeatureFlagForProducersFeebreakdown()).ReturnsAsync(true);

        // Act
        var result = await SystemUnderTest.GetMemberCount(submissionId, iscomplianceScheme, It.IsAny<Guid?>());

        // Assert
        result.Should().Be(memberCount);
    }

    [Test]
    public async Task GetMemberCount_ReturnsMemberCountOfTwo_WhenAComplianceSchemeViewsFee()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var memberCount = 2;
        var iscomplianceScheme = true;

        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ReturnsAsync(new PackagingResubmissionMemberDetails() { MemberCount = memberCount });

        // Act
        var result = await SystemUnderTest.GetMemberCount(submissionId, iscomplianceScheme, It.IsAny<Guid?>());

        // Assert
        result.Should().Be(memberCount);
    }

    [Test]
    public async Task GetMemberCount_ReturnsMemberCountOfZero_WhenNullReturnedFromServiceForComplianceScheme()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var memberCount = 2;
        var iscomplianceScheme = true;

        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>()));

        // Act
        var result = await SystemUnderTest.GetMemberCount(submissionId, iscomplianceScheme, It.IsAny<Guid?>());

        // Assert
        result.Should().Be(default(int));
    }

    [Test]
    public async Task GetMemberCount_ThrowsException_WhenNoResubmissionsFoundForComplianceScheme()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var iscomplianceScheme = true;

        ResubmissionApplicationService.Setup(x => x.GetPackagingResubmissionMemberDetails(It.IsAny<PackagingResubmissionMemberRequest>())).ThrowsAsync(new HttpRequestException("message", null, System.Net.HttpStatusCode.PreconditionRequired));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await SystemUnderTest.GetMemberCount(submissionId, iscomplianceScheme, It.IsAny<Guid?>()));
    }
    
    [Test]
    public async Task GetSubmissionHistory_ReturnsValidHistoryCount_WhenAValidSubmissionIsPassed()
    {
        // Arrange
        var submission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") };
        var organisationId = Guid.NewGuid();
        var submissionPeriodId = new SubmissionPeriodId
        {
            SubmissionId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"),
            Year = DateTime.Now.Year,
        };
        var history1 = new SubmissionHistory()
        {
            FileName = "history1"
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory> { history1 };

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);
        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        // Act
        var result = await SystemUnderTest.GetSubmissionHistory(submission, organisationId, It.IsAny<Guid?>());

        // Assert
        result.Should().Be(submissionHistories.Count);
    }

    [Test]
    public async Task GetSubmissionHistory_ReturnsDefaultHistoryCount_WhenAInValidSubmissionIsPassed()
    {
        // Arrange
        var submission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") };
        var organisationId = Guid.NewGuid();
        var submissionPeriodId = new SubmissionPeriodId
        {
            Year = DateTime.Now.Year,
        };
        var history1 = new SubmissionHistory()
        {
            FileName = "history1"
        };
        var submissionPeriodIds = new List<SubmissionPeriodId> { submissionPeriodId };
        var submissionHistories = new List<SubmissionHistory>();

        ResubmissionApplicationService.Setup(x => x.GetSubmissionIdsAsync(It.IsAny<Guid>(), SubmissionType.Producer, It.IsAny<Guid?>(), It.IsAny<int?>())).ReturnsAsync(submissionPeriodIds);
        ResubmissionApplicationService.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(submissionHistories);

        // Act
        var result = await SystemUnderTest.GetSubmissionHistory(submission, organisationId, It.IsAny<Guid?>());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FileUploadResubmissionConfirmation_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var submittedAt = DateTime.UtcNow.AddDays(-2);
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession { PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = submittedAt } } },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = "Test Organisation",
                        OrganisationRole = OrganisationRoles.Producer,
                        NationId = 1
                    }
                }
                }
            }));

        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            LastSubmittedFile = new LastSubmittedFileDetails() { SubmittedDateTime = submittedAt },
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
          It.IsAny<Organisation>(),
          It.IsAny<List<string>>(),
          It.IsAny<Guid?>()))
          .ReturnsAsync(resubmissionApplicationDetailsCollection);

        // Act
        var result = await SystemUnderTest.FileUploadResubmissionConfirmation() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data{PagePaths.FileUploadSubmissionDeclaration}");
        result.Model.Should().BeOfType<FileUploadResubmissionConfirmationViewModel>();

        result.Model.As<FileUploadResubmissionConfirmationViewModel>().Should().BeEquivalentTo(new FileUploadResubmissionConfirmationViewModel
        {
            OrganisationRole = _userData.Organisations[0].OrganisationRole,
            SubmittedAt = submittedAt.ToReadableDate()
        });
    }

    [Test]
    public async Task FileUploadResubmissionConfirmation_RedirectsToSublandingPage_WhenSubmissionIsNotFound()
    {
        // Arrange
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession()));

        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
          It.IsAny<Organisation>(),
          It.IsAny<List<string>>(),
          It.IsAny<Guid?>()))
          .ReturnsAsync(resubmissionApplicationDetailsCollection);

        // Act
        var result = await SystemUnderTest.FileUploadResubmissionConfirmation() as RedirectToActionResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");
    }

    [Test]
    public async Task FileUploadResubmissionConfirmation__GetPackagingDataResubmissionApplicationDetails_ShouldBeCalledOnce()
    {
        // Arrange
        var submittedAt = DateTime.UtcNow.AddDays(-2);
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession { PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = submittedAt } } },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = "Test Organisation",
                        OrganisationRole = OrganisationRoles.Producer,
                        NationId = 1
                    }
                }
                }
            }));

        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            LastSubmittedFile = new LastSubmittedFileDetails() { SubmittedDateTime = submittedAt },
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
          It.IsAny<Organisation>(),
          It.IsAny<List<string>>(),
          It.IsAny<Guid?>()))
          .ReturnsAsync(resubmissionApplicationDetailsCollection);

        // Act
        var result = await SystemUnderTest.FileUploadResubmissionConfirmation() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        ResubmissionApplicationService.Verify(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<Organisation?>(), It.IsAny<List<string>>(), It.IsAny<Guid?>()), Times.Once);
        result.Model.Should().BeOfType<FileUploadResubmissionConfirmationViewModel>();
    }

    [Test]
    public async Task FileUploadResubmissionConfirmation_ReturnsCorrectViewAndModel_ForCSOUsers()
    {
        // Arrange
        var submittedAt = DateTime.UtcNow.AddDays(-2);
        var complianceSchemeId = Guid.NewGuid();
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession() { SelectedComplianceScheme = new ComplianceSchemeDto() { Id = complianceSchemeId } },
                PomResubmissionSession = new PackagingReSubmissionSession { PomSubmission = new PomSubmission() { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), SubmittedDateTime = submittedAt } } },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = "Test Organisation",
                        OrganisationRole = OrganisationRoles.Producer,
                        NationId = 1
                    }
                }
                }
            }));

        var resubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            LastSubmittedFile = new LastSubmittedFileDetails() { SubmittedDateTime = submittedAt },
            SynapseResponse = new SynapseResponse
            {
                IsFileSynced = true
            }
        };
        var resubmissionApplicationDetailsCollection = new List<PackagingResubmissionApplicationDetails> { resubmissionApplicationDetails };

        ResubmissionApplicationService.Setup(x => x.GetPackagingDataResubmissionApplicationDetails(
          It.IsAny<Organisation>(),
          It.IsAny<List<string>>(),
          complianceSchemeId))
          .ReturnsAsync(resubmissionApplicationDetailsCollection);

        // Act
        var result = await SystemUnderTest.FileUploadResubmissionConfirmation() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data{PagePaths.FileUploadSubmissionDeclaration}");
        result.Model.Should().BeOfType<FileUploadResubmissionConfirmationViewModel>();

        result.Model.As<FileUploadResubmissionConfirmationViewModel>().Should().BeEquivalentTo(new FileUploadResubmissionConfirmationViewModel
        {
            OrganisationRole = _userData.Organisations[0].OrganisationRole,
            SubmittedAt = submittedAt.ToReadableDate()
        });
    }
}