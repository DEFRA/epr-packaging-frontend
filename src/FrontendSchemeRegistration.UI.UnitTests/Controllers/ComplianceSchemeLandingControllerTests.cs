namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Notification;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
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
                    OrganisationRole = "ComplianceScheme"
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
            _nullLogger,
            Options.Create(new GlobalVariables { SubmissionPeriods = _submissionPeriods, SchemeYear = 2023 }))
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
                SubmissionPeriods = _submissionPeriods.Select(period => new DatePeriod
                {
                    StartMonth = period.StartMonth,
                    EndMonth = period.EndMonth
                }).ToList()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
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
                SubmissionPeriods = _submissionPeriods.Select(period => new DatePeriod
                {
                    StartMonth = period.StartMonth,
                    EndMonth = period.EndMonth
                }).ToList()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_UserHasNominatedNotification()
    {
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
                    HasPendingNotification = false
                },
                SubmissionPeriods = _submissionPeriods.Select(period => new DatePeriod
                {
                    StartMonth = period.StartMonth,
                    EndMonth = period.EndMonth
                }).ToList()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_UserHasPendingApprovalNotification()
    {
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
                    HasPendingNotification = true
                },
                SubmissionPeriods = _submissionPeriods.Select(period => new DatePeriod
                {
                    StartMonth = period.StartMonth,
                    EndMonth = period.EndMonth
                }).ToList()
            });

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
            It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task Post_UpdatesSessionAndRedirectsToGet_WhenSelectedComplianceSchemeIdIsValidAndIsFirstCreated()
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
        capturedSession.RegistrationSession.IsSelectedComplianceSchemeFirstCreated.Should().BeTrue();
    }

    [Test]
    public async Task Post_UpdatesSessionAndRedirectsToGet_WhenSelectedComplianceSchemeIdIsValidAndIsNotFirstCreated()
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
        var result = await _systemUnderTest.Post(_complianceSchemeTwoId.ToString()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeLandingController.Get));

        capturedSession.RegistrationSession.SelectedComplianceScheme.Should().BeEquivalentTo(complianceSchemes[1]);
        capturedSession.RegistrationSession.IsSelectedComplianceSchemeFirstCreated.Should().BeFalse();
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
