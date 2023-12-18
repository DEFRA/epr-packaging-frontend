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
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;
using Organisation = EPR.Common.Authorization.Models.Organisation;

public class UploadNewFileToSubmitControllerTests
{
    private const string ViewName = "UploadNewFileToSubmit";
    private const string SubmissionPeriod = "submissionPeriod";
    private const string OrganisationName = "Org Name Ltd";
    private static readonly Guid OrganisationId = Guid.NewGuid();

    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private UploadNewFileToSubmitController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(new PersonDto
            {
                FirstName = "Test",
                LastName = "Name",
                ContactEmail = "test@email.com"
            });
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = SubmissionPeriod,
                    Journey = new List<string>(),
                    FileId = Guid.NewGuid()
                },
                UserData = new UserData
                {
                    Id = Guid.NewGuid(),
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            Id = OrganisationId,
                            Name = OrganisationName,
                            OrganisationRole = "Producer"
                        }
                    }
                }
            });
        _systemUnderTest = new UploadNewFileToSubmitController(
            _submissionServiceMock.Object, _userAccountServiceMock.Object, _sessionManagerMock.Object);
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", Guid.NewGuid().ToString() },
                    }),
                },
                User = _claimsPrincipalMock.Object,
                Session = Mock.Of<ISession>()
            }
        };
        _systemUnderTest.Url = Mock.Of<IUrlHelper>();
    }

    [Test]
    public async Task Get_ReturnsToFileUploadSubLanding_WhenSessionIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Get_ReturnsToFileUploadSubLanding_WhenOrganisationRoleIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = SubmissionPeriod,
                    Journey = new List<string>(),
                    FileId = Guid.NewGuid()
                },
                UserData = new UserData
                {
                    Id = Guid.NewGuid(),
                    Organisations = new List<Organisation>()
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task Get_ReturnsToFileUploadSubLanding_WhenSubmissionIsNull()
    {
        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    [TestCase(ServiceRoles.ApprovedPerson, true)]
    [TestCase(ServiceRoles.DelegatedPerson, true)]
    [TestCase(ServiceRoles.BasicUser, false)]
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenFileUploadNoSubmission(string serviceRole, bool isApprovedOrDelegated)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "UploadedFile",
                FileUploadDateTime = DateTime.Now,
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.Status.Should().Be(Status.FileUploadedButNothingSubmitted);
        model?.HasNewFileUploaded.Should().BeFalse();
        model?.IsApprovedOrDelegatedUser.Should().Be(isApprovedOrDelegated);
    }

    [Test]
    [TestCase(ServiceRoles.ApprovedPerson, true)]
    [TestCase(ServiceRoles.DelegatedPerson, true)]
    [TestCase(ServiceRoles.BasicUser, false)]
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmitted(string serviceRole, bool isApprovedOrDelegated)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "UploadedFile",
                FileUploadDateTime = DateTime.Now,
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "SubmittedFile",
                SubmittedDateTime = DateTime.Now.AddDays(1),
                SubmittedBy = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.Status.Should().Be(Status.FileSubmitted);
        model?.HasNewFileUploaded.Should().BeFalse();
        model?.IsApprovedOrDelegatedUser.Should().Be(isApprovedOrDelegated);
    }

    [Test]
    [TestCase(ServiceRoles.ApprovedPerson, true)]
    [TestCase(ServiceRoles.DelegatedPerson, true)]
    [TestCase(ServiceRoles.BasicUser, false)]
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmittedAndFileReUploaded(string serviceRole, bool isApprovedOrDelegated)
    {
        // Arrange
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "UploadedFile",
                FileUploadDateTime = DateTime.Now.AddDays(1),
                UploadedBy = Guid.NewGuid(),
                FileId = Guid.NewGuid()
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "SubmittedFile",
                SubmittedDateTime = DateTime.Now,
                SubmittedBy = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.Status.Should().Be(Status.FileSubmittedAndNewFileUploadedButNotSubmitted);
        model?.HasNewFileUploaded.Should().BeTrue();
        model?.IsApprovedOrDelegatedUser.Should().Be(isApprovedOrDelegated);
    }

    [Test]
    [TestCase(ServiceRoles.ApprovedPerson, true)]
    [TestCase(ServiceRoles.DelegatedPerson, true)]
    [TestCase(ServiceRoles.BasicUser, false)]
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmittedByEqualsUploadedBy(string serviceRole, bool isApprovedOrDelegated)
    {
        // Arrange
        var uploadedBy = Guid.NewGuid();
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "UploadedFile",
                FileUploadDateTime = DateTime.Now.AddDays(1),
                UploadedBy = uploadedBy,
                FileId = Guid.NewGuid()
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "SubmittedFile",
                SubmittedDateTime = DateTime.Now,
                SubmittedBy = uploadedBy
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.SubmittedBy.Should().Be(model?.UploadedBy);
    }

    private static List<Claim> CreateUserDataClaim(string serviceRole, string organisationRole)
    {
        var userData = new UserData
        {
            ServiceRole = serviceRole,
            Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = OrganisationName,
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