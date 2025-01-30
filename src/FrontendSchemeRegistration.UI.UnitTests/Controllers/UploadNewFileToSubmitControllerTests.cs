namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.Constants;
using Application.DTOs.Submission;
using Application.DTOs.UserAccount;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
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
    private Mock<IFeatureManager> _featureManager;
    private UploadNewFileToSubmitController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _featureManager = new Mock<IFeatureManager>();
        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(new PersonDto
            {
                FirstName = "Test",
                LastName = "Name",
                ContactEmail = "test@email.com"
            });

        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>()))
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
                    Journey = new List<string> { PagePaths.FileUploadSubLanding },
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

        _submissionServiceMock.Setup(x => x.GetDecisionAsync<PomDecision>(
                null, It.IsAny<Guid>(), It.IsAny<SubmissionType>()))
            .ReturnsAsync(new PomDecision());
        _systemUnderTest = new UploadNewFileToSubmitController(
            _submissionServiceMock.Object, _userAccountServiceMock.Object, _sessionManagerMock.Object,
            _featureManager.Object);
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
    public async Task Get_ReturnsToFileUploadSubLanding_WhenRequestQueryHasNoSubmissionId()
    {
        // Arrange (with no submissionId in query)
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    { }),
                },
                User = _claimsPrincipalMock.Object,
                Session = Mock.Of<ISession>()
            }
        };

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
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenFileUploadNoSubmission(
        string serviceRole,
        bool isApprovedOrDelegated)
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

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
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmitted(
        string serviceRole,
        bool isApprovedOrDelegated)
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

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
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmittedAndFileReUploaded(
        string serviceRole,
        bool isApprovedOrDelegated)
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

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
    public async Task Get_ReturnsUploadNewFileToSubmitController_WhenSubmittedByEqualsUploadedBy(
        string serviceRole,
        bool isApprovedOrDelegated)
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var claims = CreateUserDataClaim(serviceRole, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.SubmittedBy.Should().Be(model?.UploadedBy);
    }

    [Test]
    public async Task Get_ReturnsUploadNewFileToSubmitWithStatusNone_WhenSubmittedAtEqualsUploadedAt()
    {
        // Arrange
        var uploadDate = new DateTime(1970, 1, 1);
        var uploadedBy = Guid.NewGuid();
        var submission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileName = "UploadedFile",
                FileUploadDateTime = uploadDate,
                UploadedBy = uploadedBy,
                FileId = Guid.NewGuid()
            },
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileName = "SubmittedFile",
                SubmittedDateTime = uploadDate,
                SubmittedBy = uploadedBy
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = (UploadNewFileToSubmitViewModel)result.ViewData.Model;
        model?.SubmittedBy.Should().Be(model?.UploadedBy);
        model?.Status.Should().Be(Status.None);
    }

    [Test]
    public async Task Get_ReturnsUploadNewFileToSubmit_WhenFileUploadSubLandingIsInSessionHistory()
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = SubmissionPeriod,
                    Journey = new List<string> { PagePaths.FileUploadSubLanding },
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

        // Act
        var result = await _systemUnderTest.Get();

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as ViewResult;
        checkResult.Should().NotBeNull();
        checkResult.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task
        Get_RedirectsToFileUploadSubLanding_WithFeatureFlagFalse_WhenFileUploadSubLandingIsNotInSessionHistory()
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

        _featureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission))).ReturnsAsync(false);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

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

        // Act
        var result = await _systemUnderTest.Get();

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as RedirectToActionResult;
        checkResult.Should().NotBeNull();
        checkResult.ActionName.Should().Be("Get");
        checkResult.ControllerName.Should().Be("FileUploadSubLanding");
    }

    [Test]
    public async Task
        Get_RedirectsToFileUploadSubLanding_WithFeatureFlagTrue_WhenFileUploadSubLandingIsNotInSessionHistory()
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
        _featureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission))).ReturnsAsync(true);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

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

        // Act
        var result = await _systemUnderTest.Get();

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as RedirectToActionResult;
        checkResult.Should().NotBeNull();
        checkResult.ActionName.Should().Be("Get");
        checkResult.ControllerName.Should().Be("FileUploadSubLanding");
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