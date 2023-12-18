namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

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

public class FileUploadCompanyDetailsSubLandingControllerTests
{
    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = "Data period 1",
            Deadline = DateTime.Today,
            ActiveFrom = DateTime.Today
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 2",
            Deadline = DateTime.Today.AddDays(5),
            ActiveFrom = DateTime.Today.AddDays(5)
        }
    };

    private FileUploadCompanyDetailsSubLandingController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();

        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = "Approved Person",
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
                },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    }
                }
            });

        _systemUnderTest = new FileUploadCompanyDetailsSubLandingController(
            _submissionServiceMock.Object,
            _sessionManagerMock.Object,
            Options.Create(new GlobalVariables { BasePath = "path", SubmissionPeriods = _submissionPeriods }));

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenCalled()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id, It.IsAny<bool?>()))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod
                }
            });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = selectedComplianceScheme,
                },
                UserData = new UserData
                {
                    Organisations =
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.ComplianceScheme
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");
        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    Status = SubmissionPeriodStatus.NotStarted
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet
                }
            },
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenSelectedComplianceIsNull()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod
                }
            });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null,
                },
                UserData = new UserData
                {
                    Organisations =
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");
        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            SubmissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    Status = SubmissionPeriodStatus.NotStarted
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet
                }
            },
            OrganisationRole = OrganisationRoles.Producer
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenOrganisationRolesIsNull()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod
                }
            });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        var actionResult = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        actionResult.ActionName.Should().Be("LandingPage");
    }

    [Test]
    public async Task Post_RedirectsToGetAction_IfSubmissionPeriodFromPayloadIsInvalid()
    {
        // Arrange
        const string submissionPeriod = "invalid";

        // Act
        var result = await _systemUnderTest.Post(submissionPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsSubLandingController.Get));
    }

    [Test]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToFileUploadCompanyDetailsPage_WhenSubmissionExists()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            HasValidFile = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "RegData",
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsUploadDatetime = DateTime.Now,
                BrandsFileName = string.Empty,
                BrandsUploadedBy = null,
                BrandsUploadDatetime = null,
                PartnershipsFileName = string.Empty,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            }
        };
        var sessionObj = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData { ServiceRole = "Basic User" }
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<RegistrationSubmission> { submission });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(sessionObj);

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileReUploadCompanyDetailsConfirmationController.Get));
        result.ControllerName.Should().Be(nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName());
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id);

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    s =>
                        s.RegistrationSession.SubmissionPeriod == _submissionPeriods[0].DataPeriod
                        && s.RegistrationSession.Journey.Count == 1 && s.RegistrationSession.Journey[0] == PagePaths.FileUploadCompanyDetailsSubLanding)),
            Times.Once());
    }

    [Test]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToFileUploadCompanyDetailsWithNoSubmissionIdQueryParam_WhenSubmissionDoesNotExist()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null, It.IsAny<bool?>()))
            .ReturnsAsync(new List<RegistrationSubmission>());
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadCompanyDetailsController).RemoveControllerFromName());
        result.RouteValues.Should().BeNull();

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(
                    s =>
                        s.RegistrationSession.SubmissionPeriod == _submissionPeriods[0].DataPeriod
                        && s.RegistrationSession.Journey.Count == 1 && s.RegistrationSession.Journey[0] == PagePaths.FileUploadCompanyDetailsSubLanding)),
            Times.Once);
    }
}