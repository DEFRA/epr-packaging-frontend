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
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
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
            ActiveFrom = DateTime.Today,
            Year = "2023",
            StartMonth = "January",
            EndMonth = "June", 
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

    private FileUploadCompanyDetailsSubLandingController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IFeatureManager> _featureManagerMock;

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
        _featureManagerMock = new Mock<IFeatureManager>();

        _systemUnderTest = new FileUploadCompanyDetailsSubLandingController(
            _submissionServiceMock.Object,
            _sessionManagerMock.Object,
            Options.Create(new GlobalVariables { BasePath = "path", SubmissionPeriods = _submissionPeriods }),
            _featureManagerMock.Object);

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
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id))
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

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Status = SubmissionPeriodStatus.NotStarted,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenSelectedComplianceIsNull()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null))
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

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    Status = SubmissionPeriodStatus.NotStarted,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");
        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.Producer
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenOrganisationRolesIsNull()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null))
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
    public async Task Get_ReturnsCorrectViewModel_WhenShowRegistrationResubmissionIsEnabled()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod,
                    IsSubmitted = true
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
        _featureManagerMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Status = SubmissionPeriodStatus.SubmittedToRegulator,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenShowRegistrationResubmissionIsEnabled_And_DecisionIsApproved()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod,
                    IsSubmitted = true
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
        _featureManagerMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);
        _submissionServiceMock.Setup(x => x.GetDecisionAsync<RegistrationDecision>(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<SubmissionType>()))
        .ReturnsAsync(new RegistrationDecision
        {
            Decision = "Approved"
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Status = SubmissionPeriodStatus.AcceptedByRegulator,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenShowRegistrationResubmissionIsEnabled_And_DecisionIsRjected()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod,
                    IsSubmitted = true
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
        _featureManagerMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);
        _submissionServiceMock.Setup(x => x.GetDecisionAsync<RegistrationDecision>(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<SubmissionType>()))
        .ReturnsAsync(new RegistrationDecision
        {
            Decision = "Rejected"
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Status = SubmissionPeriodStatus.RejectedByRegulator,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewModel_WhenShowRegistrationResubmissionIsEnabled_And_DecisionIsAccepted()
    {
        // Arrange
        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Acme Org Ltd" };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), 1, selectedComplianceScheme.Id))
            .ReturnsAsync(new List<RegistrationSubmission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HasValidFile = true,
                    SubmissionPeriod = _submissionPeriods[0].DataPeriod,
                    IsSubmitted = true
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
        _featureManagerMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);
        _submissionServiceMock.Setup(x => x.GetDecisionAsync<RegistrationDecision>(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<SubmissionType>()))
        .ReturnsAsync(new RegistrationDecision
        {
            Decision = "Accepted"
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSubLanding");

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>
            {
                new()
                {
                    DataPeriod = _submissionPeriods[0].DataPeriod,
                    Deadline = _submissionPeriods.ElementAt(0).Deadline,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(0).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(0).EndMonth,
                    Status = SubmissionPeriodStatus.AcceptedByRegulator,
                    DatePeriodYear = _submissionPeriods[0].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(0).LocalisedShortMonth(MonthType.End),
                },
                new()
                {
                    DataPeriod = _submissionPeriods.ElementAt(1).DataPeriod,
                    DatePeriodStartMonth = _submissionPeriods.ElementAt(1).StartMonth,
                    DatePeriodEndMonth = _submissionPeriods.ElementAt(1).EndMonth,
                    Deadline = _submissionPeriods.ElementAt(1).Deadline,
                    Status = SubmissionPeriodStatus.CannotStartYet,
                    DatePeriodYear = _submissionPeriods[1].Year,
                    DatePeriodShortStartMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.Start),
                    DatePeriodShortEndMonth = _submissionPeriods.ElementAt(1).LocalisedShortMonth(MonthType.End),
                }
            };

        var submissionPeriodDetailGroups = submissionPeriodDetails
                        .OrderByDescending(c => c.DatePeriodYear)
                        .GroupBy(c => new { c.DatePeriodYear })
                        .Select(c => new SubmissionPeriodDetailGroup
                        {
                            DatePeriodYear = c.Key.DatePeriodYear,
                            SubmissionPeriodDetails = c.ToList(),
                            Quantity = c.Count()
                        }).ToList();

        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSubLandingViewModel
        {
            ComplianceSchemeName = selectedComplianceScheme.Name,
            SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });
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
    [TestCaseSource(nameof(Post_SavesSubmissionPeriodInSessionAndRedirectsToCorrectPage_WhenSubmissionExists_Cases))]
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsToCorrectPage_WhenSubmissionExists(
        bool isSubmitted,
        UploadedRegistrationFilesInformation lastUploadedValidFiles,
        SubmittedRegistrationFilesInformation lastSubmittedFiles,
        string serviceRole,
        string expectedActionName,
        string expectedControllerName)
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            HasValidFile = true,
            IsSubmitted = isSubmitted,
            LastUploadedValidFiles = lastUploadedValidFiles,
            LastSubmittedFiles = lastSubmittedFiles
        };
        var sessionObj = new FrontendSchemeRegistrationSession { UserData = new UserData { ServiceRole = serviceRole } };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null))
            .ReturnsAsync(new List<RegistrationSubmission> { submission });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(sessionObj);

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(expectedActionName);
        result.ControllerName.Should().Be(expectedControllerName);
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
    public async Task Post_SavesSubmissionPeriodInSessionAndRedirectsFileUploadCompanyDetailsController_WhenSubmissionNotStarted()
    {
        // Arrange
        var submission = new RegistrationSubmission { Id = Guid.NewGuid(), HasValidFile = false };
        var sessionObj = new FrontendSchemeRegistrationSession { UserData = new UserData { ServiceRole = "Basic User" } };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null))
            .ReturnsAsync(new List<RegistrationSubmission> { submission });
        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(sessionObj);

        // Act
        var result = await _systemUnderTest.Post(_submissionPeriods[0].DataPeriod) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadCompanyDetailsController).RemoveControllerFromName());

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
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(It.IsAny<List<string>>(), 1, null))
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

    [Test]
    public async Task Get_ReturnsSubmissionPeriods_CorrectOrder_WhenCalled()
    {
        // Arrange
        _submissionServiceMock
            .Setup(x => x.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<Guid?>()))
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
        var viewModel = result.Model as FileUploadCompanyDetailsSubLandingViewModel;
        viewModel.Should().NotBeNull();

        var submissionPeriodYear1 = int.Parse(viewModel.SubmissionPeriodDetailGroups[0].DatePeriodYear);
        var submissionPeriodYear2 = int.Parse(viewModel.SubmissionPeriodDetailGroups[1].DatePeriodYear);

        submissionPeriodYear1.Should().BeGreaterThanOrEqualTo(submissionPeriodYear2);
    }

    private static object[] Post_SavesSubmissionPeriodInSessionAndRedirectsToCorrectPage_WhenSubmissionExists_Cases()
    {
        const string BASIC_USER = "Basic User";
        const string DELEGATER_PERSON = "Delegated Person";

        var dateTime = new DateTime(1970, 1, 1);

        var uploadedFile = new UploadedRegistrationFilesInformation
        {
            CompanyDetailsFileId = Guid.NewGuid(),
            CompanyDetailsFileName = "RegData",
            CompanyDetailsUploadedBy = Guid.NewGuid(),
            CompanyDetailsUploadDatetime = dateTime,
            BrandsFileName = string.Empty,
            BrandsUploadedBy = null,
            BrandsUploadDatetime = null,
            PartnershipsFileName = string.Empty,
            PartnershipsUploadedBy = null,
            PartnershipsUploadDatetime = null
        };

        var submittedFile = new SubmittedRegistrationFilesInformation
        {
            SubmittedDateTime = dateTime.AddHours(1)
        };

        var reuploadedFile = new UploadedRegistrationFilesInformation
        {
            CompanyDetailsFileId = Guid.NewGuid(),
            CompanyDetailsFileName = "RegData",
            CompanyDetailsUploadedBy = Guid.NewGuid(),
            CompanyDetailsUploadDatetime = dateTime.AddHours(2),
            BrandsFileName = string.Empty,
            BrandsUploadedBy = null,
            BrandsUploadDatetime = null,
            PartnershipsFileName = string.Empty,
            PartnershipsUploadedBy = null,
            PartnershipsUploadDatetime = null
        };

        return new object[]
        {
            new object[]
            {
                false,
                uploadedFile,
                null,
                BASIC_USER,
                nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName()
            },
            new object[]
            {
                false,
                uploadedFile,
                null,
                DELEGATER_PERSON,
                nameof(ReviewCompanyDetailsController.Get),
                nameof(ReviewCompanyDetailsController).RemoveControllerFromName()
            },
            new object[]
            {
                true,
                uploadedFile,
                submittedFile,
                DELEGATER_PERSON,
                nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName()
            },
            new object[]
            {
                true,
                reuploadedFile,
                submittedFile,
                DELEGATER_PERSON,
                nameof(ReviewCompanyDetailsController.Get),
                nameof(ReviewCompanyDetailsController).RemoveControllerFromName()
            },
            new object[]
            {
                true,
                uploadedFile,
                submittedFile,
                BASIC_USER,
                nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName()
            },
            new object[]
            {
                true,
                reuploadedFile,
                submittedFile,
                BASIC_USER,
                nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName()
            }
        };
    }
}