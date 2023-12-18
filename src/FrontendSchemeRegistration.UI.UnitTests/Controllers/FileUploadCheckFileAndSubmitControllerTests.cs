namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.DTOs.Submission;
using Application.DTOs.UserAccount;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;
using Organisation = EPR.Common.Authorization.Models.Organisation;

[TestFixture]
public class FileUploadCheckFileAndSubmitControllerTests
{
    private const string ViewName = "FileUploadCheckFileAndSubmit";
    private static readonly Guid _lastValidFileUploadedByUserId = Guid.NewGuid();
    private static readonly Guid _lastSubmittedByUserId = Guid.NewGuid();
    private static readonly Guid _submissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private FileUploadCheckFileAndSubmitController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        var personThatLastUploadedValidFile = new PersonDto { FirstName = "John", LastName = "Doe" };
        _userAccountServiceMock
            .Setup(x => x.GetPersonByUserId(_lastValidFileUploadedByUserId))
            .ReturnsAsync(personThatLastUploadedValidFile);
        _systemUnderTest = new FileUploadCheckFileAndSubmitController(
            _submissionServiceMock.Object,
            _userAccountServiceMock.Object,
            _sessionManagerMock.Object,
            Mock.Of<ILogger<FileUploadCheckFileAndSubmitController>>());
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
    }

    [Test]
    public async Task Get_ReturnsToFileUploadGet_WhenSubmissionIsNull()
    {
        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected, false)]
    public async Task Get_ReturnsCorrectViewAndModel_WhenSubmissionIsNotSubmitted(string serviceRole, string enrollementStatus, bool expectedUserCanSubmit)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, enrollementStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.Should().BeEquivalentTo(new FileUploadCheckFileAndSubmitViewModel
        {
            SubmissionId = submission.Id,
            UserCanSubmit = expectedUserCanSubmit,
            LastValidFileId = submission.LastUploadedValidFile!.FileId,
            LastValidFileName = submission.LastUploadedValidFile!.FileName,
            LastValidFileUploadedBy = "John Doe",
            LastValidFileUploadDateTime = submission.LastUploadedValidFile!.FileUploadDateTime,
            OrganisationRole = OrganisationRoles.Producer,
            SubmittedBy = null,
            SubmittedDateTime = null,
            SubmittedFileName = null
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Enrolled, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected, false)]
    public async Task Get_ReturnsCorrectViewAndModel_WhenSubmissionIsSubmitted(string serviceRole, string enrolmentStatus, bool expectedUserCanSubmit)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "submitted-file.csv",
                SubmittedDateTime = DateTime.Now.AddMinutes(5),
                SubmittedBy = _lastSubmittedByUserId
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams" };
        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.Should().BeEquivalentTo(new FileUploadCheckFileAndSubmitViewModel
        {
            SubmissionId = submission.Id,
            UserCanSubmit = expectedUserCanSubmit,
            LastValidFileId = submission.LastUploadedValidFile!.FileId,
            LastValidFileName = submission.LastUploadedValidFile!.FileName,
            LastValidFileUploadedBy = "John Doe",
            LastValidFileUploadDateTime = submission.LastUploadedValidFile!.FileUploadDateTime,
            OrganisationRole = OrganisationRoles.Producer,
            SubmittedBy = "Brian Adams",
            SubmittedDateTime = submission.LastSubmittedFile.SubmittedDateTime,
            SubmittedFileName = submission.LastSubmittedFile!.FileName
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(_lastSubmittedByUserId), Times.Once);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Enrolled)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Enrolled)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Enrolled)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected)]
    public async Task Post_RedirectsToGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new FileUploadCheckFileAndSubmitViewModel();
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
    }

    [Test]
    public async Task Post_ReturnsToFileUploadGet_WhenSubmissionIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        var model = new FileUploadCheckFileAndSubmitViewModel();

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new FileUploadCheckFileAndSubmitViewModel();
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _systemUnderTest.ModelState.AddModelError("Key", "Value");

        // Act
        var result = await _systemUnderTest.Post(model) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.Should().BeEquivalentTo(new FileUploadCheckFileAndSubmitViewModel
        {
            SubmissionId = submission.Id,
            UserCanSubmit = true,
            LastValidFileId = submission.LastUploadedValidFile!.FileId,
            LastValidFileName = submission.LastUploadedValidFile!.FileName,
            LastValidFileUploadedBy = "John Doe",
            LastValidFileUploadDateTime = submission.LastUploadedValidFile!.FileUploadDateTime,
            OrganisationRole = OrganisationRoles.Producer,
            SubmittedBy = null,
            SubmittedDateTime = null,
            SubmittedFileName = null
        });
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_SubmitsSubmissionAndRedirectsToFileUploadSubmissionConfirmationGet_WhenComplianceSchemeHasSelectedYes(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { Submit = true, LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id.ToString());
        _submissionServiceMock.Verify(x => x.SubmitAsync(submission.Id, fileId), Times.Once);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubmissionErrorGet_WhenExceptionWasThrownDuringSubmission()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { Submit = true, LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.SubmitAsync(submission.Id, fileId)).ThrowsAsync(new Exception());

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionError");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id.ToString());
        _submissionServiceMock.Verify(x => x.SubmitAsync(submission.Id, fileId), Times.Once);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_RedirectsToFileUploadSubmissionDeclarationGet_WhenDirectProducerHasSelectedYes(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new FileUploadCheckFileAndSubmitViewModel { Submit = true };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionDeclaration");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id.ToString());
        _submissionServiceMock.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Post_RedirectToFileUploadSubLandingGet_WhenUserSelectsNo()
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        var model = new FileUploadCheckFileAndSubmitViewModel
        {
            Submit = false
        };

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
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
                    OrganisationRole = organisationRole
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}