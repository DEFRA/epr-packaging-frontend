﻿namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Sessions;
using UI.ViewModels;

public class FileUploadSubLandingControllerTests
{
    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = $"Jan to Jun {DateTime.Now.Year}",
            Deadline = DateTime.Parse($"1/10/{DateTime.Now.Year} 11:59:00 PM")
        },
        new SubmissionPeriod
        {
            DataPeriod = $"Jul to Dec {DateTime.Now.Year}",
            Deadline = DateTime.Parse($"1/04/{DateTime.Now.Year + 1} 11:59:00 PM")
        }
    };

    private FileUploadSubLandingController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISession> _httpContextSessionMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;

    [SetUp]
    public void SetUp()
    {
        _httpContextSessionMock = new Mock<ISession>();
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _systemUnderTest = new FileUploadSubLandingController(
            _submissionServiceMock.Object, _sessionMock.Object, Options.Create(new GlobalVariables { SubmissionPeriods = _submissionPeriods }));
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.Should().BeEquivalentTo(new FileUploadSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods[0].Deadline,
                    Status = SubmissionPeriodStatus.FileUploaded
                },
                new()
                {
                    DataPeriod = _submissionPeriods[1].DataPeriod,
                    Deadline = _submissionPeriods[1].Deadline,
                    Status = SubmissionPeriodStatus.NotStarted
                }
            },
            OrganisationRole = organisationRole
        });
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
                ActiveFrom = DateTime.Parse($"1/10/{DateTime.Now.Year + 1} 11:59:00 PM")
            },
            new()
            {
                DataPeriod = $"Jul to Dec {DateTime.Now.Year + 1}",
                Deadline = DateTime.Parse($"1/04/{DateTime.Now.Year + 2} 11:59:00 PM"),
                ActiveFrom = DateTime.Parse($"1/10/{DateTime.Now.Year + 1} 11:59:00 PM")
            }
        };

        _systemUnderTest = new FileUploadSubLandingController(
            _submissionServiceMock.Object, _sessionMock.Object, Options.Create(new GlobalVariables { SubmissionPeriods = submissionPeriods }));
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetails.Should()
            .AllSatisfy(x => x.Status.Should().Be(SubmissionPeriodStatus.CannotStartYet));
    }

    [Test]
    [TestCase(OrganisationRoles.Producer)]
    [TestCase(OrganisationRoles.ComplianceScheme)]
    public async Task Get_ShowsNotStarted_WhenNoFileWasUploadedBefore(string organisationRole)
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock.Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.NotStarted);
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.FileUploaded);
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
        result.Model.As<FileUploadSubLandingViewModel>().SubmissionPeriodDetails[0].Status.Should().Be(SubmissionPeriodStatus.SubmittedToRegulator);
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
                It.IsAny<List<string>>(), 2, selectedComplianceScheme.Id, It.IsAny<bool?>()))
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
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
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
            Times.Once);
    }

    [Test]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToFileUploadWithNoSubmissionIdQueryParam_WhenSubmissionDoesNotExist()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<PomSubmission>(
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
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
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
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
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
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
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
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
                It.IsAny<List<string>>(), 2, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<PomSubmission> { submission });
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    SubmissionPeriod = submissionPeriod
                }
            });

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCheckFileAndSubmitController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName());
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);
    }
}