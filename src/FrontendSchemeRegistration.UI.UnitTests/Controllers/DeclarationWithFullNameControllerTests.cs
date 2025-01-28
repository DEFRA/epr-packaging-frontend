namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.ViewModels;

[TestFixture]
public class DeclarationWithFullNameControllerTests
{
    private const string ViewName = "DeclarationWithFullName";
    private const string OrganisationName = "Org Name Ltd";
    private const string DeclarationName = "Test Name";
    private static readonly Guid _submissionId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private DeclarationWithFullNameController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _systemUnderTest = new DeclarationWithFullNameController(_submissionServiceMock.Object, new NullLogger<DeclarationWithFullNameController>());
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
                User = _claimsPrincipalMock.Object
            },
        };
        _systemUnderTest.Url = Mock.Of<IUrlHelper>();
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

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("ReviewCompanyDetails");
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
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

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
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
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
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
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
            HasValidFile = true
        };
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString()
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("CompanyDetailsConfirmation");
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
            .Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), null))
            .ThrowsAsync(new Exception());

        var submissionDeclarationRequest = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName
        };

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

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
        var result = await _systemUnderTest.Post(submissionDeclarationRequest) as RedirectToActionResult;

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