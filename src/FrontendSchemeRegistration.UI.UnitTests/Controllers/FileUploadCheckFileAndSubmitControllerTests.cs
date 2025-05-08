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
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
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
    private Mock<IRegulatorService> _regulatorServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private FileUploadCheckFileAndSubmitController _systemUnderTest;
    private Mock<IUrlHelper> _urlHelperMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUploadSubLanding);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _regulatorServiceMock = new Mock<IRegulatorService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _featureManagerMock = new Mock<IFeatureManager>();
        var personThatLastUploadedValidFile = new PersonDto { FirstName = "John", LastName = "Doe" };
        _userAccountServiceMock
            .Setup(x => x.GetPersonByUserId(_lastValidFileUploadedByUserId))
            .ReturnsAsync(personThatLastUploadedValidFile);

        _userAccountServiceMock
            .Setup(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId))
            .ReturnsAsync(personThatLastUploadedValidFile);

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission)))
            .ReturnsAsync(true);

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
            .ReturnsAsync(true);

        _systemUnderTest = new FileUploadCheckFileAndSubmitController(
            _submissionServiceMock.Object,
            _userAccountServiceMock.Object,
            _sessionManagerMock.Object,
            _regulatorServiceMock.Object,
            _featureManagerMock.Object,
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

        _systemUnderTest.Url = _urlHelperMock.Object;
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
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
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
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
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
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
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
            SubmittedFileName = submission.LastSubmittedFile!.FileName,
            IsSubmittedByUserDeleted = false
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastSubmittedByUserId), Times.Once);
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
    public async Task Get_ReturnsCorrectViewAndModel_WhenSubmissionIsSubmitted_Resbumitted_Journey(string serviceRole, string enrolmentStatus, bool expectedUserCanSubmit)
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

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PomResubmissionSession = new PackagingReSubmissionSession
                {
                    IsPomResubmissionJourney = true
                }
            });

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams" };
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
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
            SubmittedFileName = submission.LastSubmittedFile!.FileName,
            IsSubmittedByUserDeleted = false
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastSubmittedByUserId), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsDeletedUsers()
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

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams", IsDeleted = true };
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
        result.Model.Should().BeEquivalentTo(new FileUploadCheckFileAndSubmitViewModel
        {
            SubmissionId = submission.Id,
            UserCanSubmit = true,
            LastValidFileId = submission.LastUploadedValidFile!.FileId,
            LastValidFileName = submission.LastUploadedValidFile!.FileName,
            LastValidFileUploadedBy = "John Doe",
            LastValidFileUploadDateTime = submission.LastUploadedValidFile!.FileUploadDateTime,
            OrganisationRole = OrganisationRoles.Producer,
            SubmittedBy = "Brian Adams",
            SubmittedDateTime = submission.LastSubmittedFile.SubmittedDateTime,
            SubmittedFileName = submission.LastSubmittedFile!.FileName,
            IsSubmittedByUserDeleted = true
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(_lastSubmittedByUserId), Times.Once);
    }

    
    [Test]
    public async Task Get_PopulatesCorrectSubmittedByUser_WhenSubmittedByAndUploadedByMatch()
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
                SubmittedBy = _lastValidFileUploadedByUserId
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams", IsDeleted = false };
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);
        _userAccountServiceMock.Setup(x=>x.GetAllPersonByUserId(_lastValidFileUploadedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        (result.Model as FileUploadCheckFileAndSubmitViewModel).SubmittedBy.Should().Be("Brian Adams");
        (result.Model as FileUploadCheckFileAndSubmitViewModel).IsSubmittedByUserDeleted.Should().BeFalse();
    }

    [Test]
    public async Task Get_PopulatesCorrectSubmittedByUser_WhenSubmittedByAndUploadedByDoNotMatch()
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

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams", IsDeleted = false };
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        (result.Model as FileUploadCheckFileAndSubmitViewModel).SubmittedBy.Should().Be("Brian Adams");
        (result.Model as FileUploadCheckFileAndSubmitViewModel).IsSubmittedByUserDeleted.Should().BeFalse();
    }

    [Test]
    public async Task Get_PopulatesIsSubmittedByUserDeletedCorrectly()
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

        var personThatLastSubmitted = new PersonDto { FirstName = "Brian", LastName = "Adams", IsDeleted = true };
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastSubmittedByUserId)).ReturnsAsync(personThatLastSubmitted);
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(_lastValidFileUploadedByUserId)).ReturnsAsync(personThatLastSubmitted);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        (result.Model as FileUploadCheckFileAndSubmitViewModel).IsSubmittedByUserDeleted.Should().BeTrue();
    }

    [Test]
    public async Task Get_ReturnsRedirectToFileUploadSublanding_WhenPageIsAccessedWithoutUploadingNewFile()
    {
        // Arrange
        var fileId = Guid.NewGuid();

        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation { FileId = fileId },
            LastSubmittedFile = new SubmittedFileInformation { FileId = fileId }
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_SendsResubmissionEmail_WhenUserIsComplianceScheme()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
            },
            SubmissionPeriod = "Jan to Jun 2023"
        };

        var input = new ResubmissionEmailRequestModel
        {
            OrganisationNumber = "123456",
            ProducerOrganisationName = "Compliance Scheme Name",
            SubmissionPeriod = "Jan to Jun 2023",
            NationId = 2,
            IsComplianceScheme = true,
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto
                    {
                        Name = "Compliance Scheme Name",
                        NationId = 2,
                    }
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                    }
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id.ToString());
        _regulatorServiceMock.Verify(
            x => x.SendRegulatorResubmissionEmail(
            It.Is<ResubmissionEmailRequestModel>(x => x.OrganisationNumber == input.OrganisationNumber
                && x.ProducerOrganisationName == input.ProducerOrganisationName
                && x.SubmissionPeriod == input.SubmissionPeriod
                && x.NationId == input.NationId
                && x.IsComplianceScheme == input.IsComplianceScheme
                && x.ComplianceSchemeName == input.ComplianceSchemeName
                && x.ComplianceSchemePersonName == input.ComplianceSchemePersonName)), Times.Once);
    }

    [Test]
    public async Task Post_SendsResubmissionEmail_WhenUserIsComplianceSchemeIsNull()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                    }
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submission.Id.ToString());
        _regulatorServiceMock.Verify(
            x => x.SendRegulatorResubmissionEmail(
            It.Is<ResubmissionEmailRequestModel>(x => x.OrganisationNumber == input.OrganisationNumber
                && x.ProducerOrganisationName == input.ProducerOrganisationName
                && x.SubmissionPeriod == input.SubmissionPeriod
                && x.NationId == input.NationId
                && x.IsComplianceScheme == input.IsComplianceScheme
                && x.ComplianceSchemeName == input.ComplianceSchemeName
                && x.ComplianceSchemePersonName == input.ComplianceSchemePersonName)), Times.Once);
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
    public async Task Post_SubmitsSubmissionAndRedirectsToFileUploadSubmissionConfirmationGet_WhenComplianceSchemeSubmits(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
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
        _sessionManagerMock
           .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
           .ReturnsAsync(new FrontendSchemeRegistrationSession
           {
               RegistrationSession = new RegistrationSession
               {
                   SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto
                   {
                       Name = "Compliance Scheme Name",
                       NationId = 2,
                   }
               },
               UserData = new UserData
               {
                   Organisations = new List<Organisation>
                   {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                   }
               }
           });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);

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
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
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
    public async Task Post_RedirectsToFileUploadSubmissionDeclarationGet_WhenDirectProducerContinues(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new FileUploadCheckFileAndSubmitViewModel();
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
    public async Task Post_ReturnsRedirectToFileUploadSublanding_WhenSubmittedWithoutUploadingNewFile()
    {
        // Arrange
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        var fileId = Guid.NewGuid();

        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation { FileId = fileId },
            LastSubmittedFile = new SubmittedFileInformation { FileId = fileId }
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var model = new FileUploadCheckFileAndSubmitViewModel();

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubLanding_WhenOrganisationIdIsNull()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubLanding_When_OrganisationIdIsNull()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubmissionConfirmation_When_SubmissionIsFalse()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                    }
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubmissionConfirmation_When_IsAnySubmissionAcceptedForDataPeriodIsFalse()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                    }
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(false);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubmissionConfirmation");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadResubmissionConfirmation_When_IsAnySubmissionAcceptedForDataPeriodIsTrue()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var model = new FileUploadCheckFileAndSubmitViewModel { LastValidFileId = fileId };
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = _lastValidFileUploadedByUserId,
                FileUploadDateTime = DateTime.Now,
                FileId = fileId
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "last-valid-file.csv",
                FileId = Guid.NewGuid()
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
            ComplianceSchemeName = "Organisation Name",
            ComplianceSchemePersonName = "First Last"
        };

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new Organisation()
                        {
                            Id = Guid.NewGuid(),
                        }
                    }
                }
            });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _submissionServiceMock.Setup(x => x.IsAnySubmissionAcceptedForDataPeriod(submission, It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(true);
        _regulatorServiceMock.Setup(x => x.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>())).ReturnsAsync("notificationId");

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("FileUploadResubmissionConfirmation");
        result.ControllerName.Should().Be("PackagingDataResubmission");
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
                    Name = "Organisation Name",
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