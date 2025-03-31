namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.RequestModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
using Moq;
using RequestModels;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadSubmissionDeclarationControllerTests
{
    private const string ViewName = "FileUploadSubmissionDeclaration";
    private const string OrganisationName = "Org Name Ltd";
    private const string DeclarationName = "Test Name";
    private static readonly Guid _submissionId = Guid.NewGuid();
    private static readonly Guid _fileId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IRegulatorService> _regulatorServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private FileUploadSubmissionDeclarationController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _regulatorServiceMock = new Mock<IRegulatorService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _featureManagerMock = new Mock<IFeatureManager>();

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission)))
            .ReturnsAsync(true);

        _systemUnderTest = new FileUploadSubmissionDeclarationController(
            _submissionServiceMock.Object,
            _sessionManagerMock.Object,
            _regulatorServiceMock.Object,
            _featureManagerMock.Object,
            Mock.Of<ILogger<FileUploadSubmissionDeclarationController>>());

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", _submissionId.ToString() },
                    }),
                },
                User = _claimsPrincipalMock.Object,
                Session = Mock.Of<ISession>()
            },
        };
        _systemUnderTest.Url = Mock.Of<IUrlHelper>();
    }

    [Test]
    public async Task Get_ReturnsToCheckFileAndSubmit_WhenFileIdIsNull()
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = null,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenSubmissionIsNotSubmitted()
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = Guid.NewGuid(),
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.Should().BeEquivalentTo(new FileUploadSubmissionDeclarationViewModel
        {
            OrganisationName = OrganisationName,
        });
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected)]
    public async Task Get_RedirectsToFileUploadCheckFileAndSubmitGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected)]
    public async Task Post_RedirectsToFileUploadCheckFileAndSubmitGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new SubmissionDeclarationRequest();
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCheckFileAndSubmit");
    }

    [Test]
    public async Task Post_RedirectsToLandingPage_WhenSubmissionIsNull()
    {
        // Arrange
        var submissionDeclarationRequest = new SubmissionDeclarationRequest();
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_ShowsError_WhenErrorPresent()
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "FileName",
                FileUploadDateTime = DateTime.Now,
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    FileId = _fileId
                }
            });

        _systemUnderTest.ModelState.AddModelError("file", "Some error");

        var submissionDeclarationRequest = new SubmissionDeclarationRequest
        {
            DeclarationName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_ReturnsSubmissionComplete_WhenDeclarationNameIsValid(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "FileName",
                FileUploadDateTime = DateTime.Now,
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    FileId = _fileId
                }
            });

        var submissionDeclarationRequest = new SubmissionDeclarationRequest
        {
            DeclarationName = DeclarationName
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
    }

    [Test]
    public async Task Post_SendResubmissionEmail_WhenDeclarationNameIsValidAndUserHasPreviousSubmission()
    {
        // Arrange
        Guid lastValidFileUploadedByUserId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = fileId
            },
            SubmissionPeriod = "Jan to Jun 2023"
        };

        var input = new ResubmissionEmailRequestModel
        {
            OrganisationNumber = "123456",
            ProducerOrganisationName = "Org Name Ltd",
            SubmissionPeriod = "Jan to Jun 2023",
            NationId = 1,
            IsComplianceScheme = false,
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    FileId = _fileId
                }
            });

        var submissionDeclarationRequest = new SubmissionDeclarationRequest
        {
            DeclarationName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        _regulatorServiceMock.Verify(
            x => x.SendRegulatorResubmissionEmail(
            It.Is<ResubmissionEmailRequestModel>(x => x.OrganisationNumber == input.OrganisationNumber
                && x.ProducerOrganisationName == input.ProducerOrganisationName
                && x.SubmissionPeriod == input.SubmissionPeriod
                && x.NationId == input.NationId
                && x.IsComplianceScheme == input.IsComplianceScheme)), Times.Once);
    }

    [Test]
    public async Task Post_SendResubmissionEmail_WhenSelectedComplianceSchemeIsNull()
    {
        // Arrange
        Guid lastValidFileUploadedByUserId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = fileId
            },
            SubmissionPeriod = "Jan to Jun 2023"
        };

        var input = new ResubmissionEmailRequestModel
        {
            OrganisationNumber = "123456",
            ProducerOrganisationName = null,
            SubmissionPeriod = "Jan to Jun 2023",
            NationId = 0,
            IsComplianceScheme = true,
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    FileId = _fileId,
                    SelectedComplianceScheme = null
                }
            });

        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var submissionDeclarationRequest = new SubmissionDeclarationRequest
        {
            DeclarationName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        _regulatorServiceMock.Verify(
            x => x.SendRegulatorResubmissionEmail(
            It.Is<ResubmissionEmailRequestModel>(x => x.OrganisationNumber == input.OrganisationNumber
                && x.ProducerOrganisationName == input.ProducerOrganisationName
                && x.SubmissionPeriod == input.SubmissionPeriod
                && x.NationId == input.NationId
                && x.IsComplianceScheme == input.IsComplianceScheme)), Times.Once);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubmissionErrorGet_WhenExceptionOccursDuringSubmission()
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "FileName",
                FileUploadDateTime = DateTime.Now,
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);
        _submissionServiceMock
            .Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), string.Empty, false))
            .ThrowsAsync(new Exception());

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    FileId = _fileId
                }
            });

        var submissionDeclarationRequest = new SubmissionDeclarationRequest
        {
            DeclarationName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionError");
    }

    private static List<Claim> CreateUserDataClaim(string serviceRole, string enrolmentStatus, string organisationRole)
    {
        var userData = new UserData
        {
            ServiceRole = serviceRole,
            EnrolmentStatus = enrolmentStatus,
            Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganisationRole = organisationRole,
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    NationId = 1
                }
            },
            FirstName = "First",
            LastName = "Last"
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}