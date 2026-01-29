using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Enums;

[TestFixture]
public class DeclarationWithFullNameControllerTests
{
    private const string ViewName = "DeclarationWithFullName";
    private const string OrganisationName = "Org Name Ltd";
    private const string DeclarationName = "Test Name";
    private const string RegistrationReferenceNumber = "TESTREGREFNO123";
    private static readonly Guid _submissionId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private DeclarationWithFullNameController _systemUnderTest;
    private Mock<IRegistrationPeriodProvider> _registrationPeriodProviderMock;


    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = true }
            });

        _systemUnderTest = new DeclarationWithFullNameController(_submissionServiceMock.Object, _sessionManagerMock.Object, new NullLogger<DeclarationWithFullNameController>(), _registrationPeriodProviderMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object,
                User = _claimsPrincipalMock.Object
            },
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _systemUnderTest.Url = urlHelperMock.Object;
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
    public async Task Get_RedirectsToReviewCompanyDetailsGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(_submissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("ReviewCompanyDetails");
    }

    [Test]
    public async Task Get_RedirectsToFileUploadSubLandingGet_WhenSubmissionIsNull()
    {
        // Arrange
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).Returns(Task.FromResult<RegistrationSubmission>(null));

        // Act
        var result = await _systemUnderTest.Get(_submissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Get_RedirectsToFileUploadSubLandingGet_WhenSubmissionDoesNotHaveAValidFile()
    {
        // Arrange
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).Returns(Task.FromResult<RegistrationSubmission>(new RegistrationSubmission { HasValidFile = false }));

        // Act
        var result = await _systemUnderTest.Get(_submissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Get_ReturnsCorrectView_WhenIsResubussionFalse()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = false }
            });
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            },
            HasValidFile = true
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(_submissionId) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
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
    public async Task Post_RedirectsToReviewCompanyDetailsGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new DeclarationWithFullNameViewModel();
        model.RegistrationYear = DateTime.Now.Year;
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(Guid.NewGuid(), model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("ReviewCompanyDetails");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenSubmissionIsNull()
    {
        // Arrange
        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel();
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(Guid.NewGuid(), submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new DeclarationWithFullNameViewModel();
        var submission = new PomSubmission
        {
            Id = _submissionId,
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "last-valid-file.csv",
                UploadedBy = Guid.NewGuid(),
                FileUploadDateTime = DateTime.Now,
                FileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _systemUnderTest.ModelState.AddModelError("Key", "Value");

        // Act
        var result = await _systemUnderTest.Post(submission.Id, model) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task Post_ShowsError_WhenErrorPresent()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            },
            HasValidFile = true
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        _systemUnderTest.ModelState.AddModelError("file", "Some error");

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task Post_ShowsError_When_valid_Period_but_missing_App_Ref()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            },
            HasValidFile = true
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                ApplicationReferenceNumber = null,
                SubmissionPeriod = "January to December 2025"
            }
        });

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("OrganisationDetailsSubmissionFailed");
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_ReturnsSubmissionComplete_WhenDeclarationNameIsValid(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            },
            HasValidFile = true,
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                ApplicationReferenceNumber = "test",
                SubmissionPeriod = "January to December 2025"
            }
        });

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString()
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("CompanyDetailsConfirmation");
        _submissionServiceMock.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), 
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney>()), Times.Once);
    }

    [Test]
    public async Task Post_RedirectsToOrganisationDetailsSubmissionFailedGet_WhenExceptionOccursDuringSubmission()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            },
            HasValidFile = true
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);
        _submissionServiceMock
            .Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), null, false, null))
            .ThrowsAsync(new Exception());

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("OrganisationDetailsSubmissionFailed");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLanding_WhenValidationPassFalse()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            ValidationPass = false
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    private static List<Claim> CreateUserDataClaim(string serviceRole, string enrolmentStatus, string organisationRole)
    {
        var userData = new UserData
        {
            Id = _userId,
            ServiceRole = serviceRole,
            EnrolmentStatus = enrolmentStatus,
            Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganisationRole = organisationRole,
                    Name = OrganisationName
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}