using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Options.RegistrationPeriodPatterns;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.DTOs.ComplianceScheme;
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
    private Mock<IFeatureManager> _featureManagerMock;
    private Mock<IPaymentCalculationService> _paymentCalculationServiceMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        SetupSubmitDetails();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = true }
            });

        _systemUnderTest = new DeclarationWithFullNameController(_submissionServiceMock.Object, _sessionManagerMock.Object, _registrationApplicationServiceMock.Object, new NullLogger<DeclarationWithFullNameController>(), _registrationPeriodProviderMock.Object, _featureManagerMock.Object, _paymentCalculationServiceMock.Object);
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

    [Test]
    public async Task Get_IncludesRegistrationJourneyFromQueryParameter_WhenSubmissionHasNoJourney()
    {
        // Arrange
        var registrationJourney = RegistrationJourney.DirectSmallProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = false }
            });
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            RegistrationJourney = null,
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
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(_submissionId, registrationJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = result.Model.As<DeclarationWithFullNameViewModel>();
        model.RegistrationJourney.Should().Be(registrationJourney);
    }

    [Test]
    public async Task Get_UsesSubmissionRegistrationJourney_WhenBothParameterAndSubmissionHaveValues()
    {
        // Arrange
        var queryParameterJourney = RegistrationJourney.DirectSmallProducer;
        var submissionJourney = RegistrationJourney.DirectLargeProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = false }
            });
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            RegistrationJourney = submissionJourney,
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
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(_submissionId, queryParameterJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = result.Model.As<DeclarationWithFullNameViewModel>();
        model.RegistrationJourney.Should().Be(submissionJourney);
    }

    [Test]
    public async Task Get_IncludesRegistrationJourneyInViewModel_WhenQueryParameterProvidedAndIsCSO()
    {
        // Arrange
        var registrationJourney = RegistrationJourney.CsoSmallProducer;
        var registrationYear = DateTime.Now.Year;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = false }
            });
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            RegistrationJourney = registrationJourney,
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
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(registrationYear);

        // Act
        var result = await _systemUnderTest.Get(_submissionId, registrationJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = result.Model.As<DeclarationWithFullNameViewModel>();
        model.RegistrationJourney.Should().Be(registrationJourney);
        model.ShowRegistrationCaption.Should().BeTrue();
        model.IsCso.Should().BeTrue();
    }

    [Test]
    public async Task Get_IncludesSubmissionRegistrationJourneyInViewModel_WhenBothParameterAndSubmissionHaveValues()
    {
        // Arrange
        var queryParameterJourney = RegistrationJourney.DirectSmallProducer;
        var submissionJourney = RegistrationJourney.CsoLargeProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession { IsResubmission = false }
            });
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            RegistrationJourney = submissionJourney,
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
        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(_submissionId, queryParameterJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var model = result.Model.As<DeclarationWithFullNameViewModel>();
        model.RegistrationJourney.Should().Be(submissionJourney);
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
    public async Task Post_ReturnsViewWithModel_WhenModelStateInvalidAndRegistrationJourneyIsSet()
    {
        // Arrange
        var registrationJourney = RegistrationJourney.DirectSmallProducer;
        var model = new DeclarationWithFullNameViewModel
        {
            RegistrationYear = DateTime.Now.Year,
            RegistrationJourney = registrationJourney,
            FullName = DeclarationName
        };
        var submission = new RegistrationSubmission
        {
            Id = _submissionId,
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _systemUnderTest.ModelState.AddModelError("Key", "Value");

        // Act
        var result = await _systemUnderTest.Post(submission.Id, model) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        var returnedModel = result.Model.As<DeclarationWithFullNameViewModel>();
        returnedModel.RegistrationJourney.Should().Be(registrationJourney);
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
        SetupSubmitDetails(applicationReferenceNumber: null);
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

    [Test]
    public async Task Post_ShowsError_WhenSessionIsNull()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            HasValidFile = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession?)null);

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
    public async Task Post_ShowsError_WhenRegistrationSessionIsNull()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            HasValidFile = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid()
            }
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = null!
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
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("CompanyDetailsConfirmation");
        _submissionServiceMock.Verify(x => x.SubmitAsync(It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.IsAny<RegistrationSubmitContext?>()), Times.Once);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_IncludesRegistrationJourneyInRedirectToConfirmation_WhenModelHasRegistrationYear(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var registrationJourney = RegistrationJourney.DirectSmallProducer;
        var registrationYear = DateTime.Now.Year;
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
            RegistrationJourney = null
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
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            RegistrationJourney = registrationJourney,
            RegistrationYear = registrationYear
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("CompanyDetailsConfirmation");
        result.RouteValues.Should().ContainKey("registrationjourney");
        result.RouteValues["registrationjourney"].Should().Be(registrationJourney);
        result.RouteValues.Should().ContainKey("registrationyear");
        result.RouteValues["registrationyear"].ToString().Should().Be(registrationYear.ToString());
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved)]
    public async Task Post_UsesSubmissionRegistrationJourney_WhenSubmittingWithModelJourney(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var modelJourney = RegistrationJourney.DirectSmallProducer;
        var submissionJourney = RegistrationJourney.CsoSmallProducer;
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
            RegistrationJourney = submissionJourney
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
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            RegistrationJourney = modelJourney,
            RegistrationYear = DateTime.Now.Year
        };

        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(submission.Id, submissionDeclarationRequest) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("CompanyDetailsConfirmation");
        result.RouteValues.Should().ContainKey("registrationjourney");
        result.RouteValues["registrationjourney"].Should().Be(submissionJourney);
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

    private static List<Claim> CreateUserDataClaim(string serviceRole, string enrolmentStatus, string organisationRole, int? nationId = (int)Nation.England)
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
                    Name = OrganisationName,
                    NationId = nationId
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }

    [Test]
    public async Task Post_FeatureFlagOff_DoesNotCallPaymentFacade()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            SubmissionPeriod = "January to December 2026",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test" },
        });
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeParametersViaPaymentService)).ReturnsAsync(false);

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Post_FeatureFlagOn_RedirectsToDeclarationProcessing()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            SubmissionPeriod = "January to December 2026",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test" },
        });
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeParametersViaPaymentService)).ReturnsAsync(true);

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        var result = await _systemUnderTest.Post(submission.Id, request);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Get");
        ((RedirectToActionResult)result).ControllerName.Should().Be("DeclarationProcessing");
    }

    [Test]
    public async Task Post_SubmitsValuesResolvedForTheSubmission()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            SubmissionPeriod = "January to December 2025",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "PEPR17583027P1" },
        });
        SetupSubmitDetails("PEPR17583025P2", isResubmission: false, submissionPeriodId: 2, regulatorNation: "GB-ENG");

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            RegistrationYear = 2025,
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert - values resolved for the submission are sent, not the session's other-year reference.
        _registrationApplicationServiceMock.Verify(x => x.ResolveRegistrationSubmitDetailsAsync(
            It.IsAny<ISession>(), It.IsAny<Organisation>(), submission, null, 2025), Times.Once);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            submission.Id, It.IsAny<Guid>(), DeclarationName,
            "PEPR17583025P2", false, It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.SubmissionPeriodId == 2 && c.RegulatorNation == "GB-ENG")), Times.Once);
    }

    private void SetupSubmitDetails(
        string? applicationReferenceNumber = "test",
        bool isResubmission = false,
        int? submissionPeriodId = null,
        string? regulatorNation = "GB-ENG",
        bool hasPreviousApplicationSubmission = false) =>
        _registrationApplicationServiceMock
            .Setup(x => x.ResolveRegistrationSubmitDetailsAsync(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<RegistrationSubmission>(), It.IsAny<RegistrationJourney?>(), It.IsAny<int?>()))
            .ReturnsAsync(new RegistrationSubmitDetails(applicationReferenceNumber, isResubmission, submissionPeriodId, regulatorNation, hasPreviousApplicationSubmission));

    [Test]
    public async Task Post_RedirectsToOrganisationDetailsSubmissionFailed_WhenRegulatorNationCannotBeResolved()
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
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(regulatorNation: null);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test" },
        });

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            IsCso = false,
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer, nationId: null));

        // Act
        var result = await _systemUnderTest.Post(submission.Id, request) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("OrganisationDetailsSubmissionFailed");
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.IsAny<RegistrationSubmitContext?>()), Times.Never);
    }

    [Test]
    public async Task Post_FirstTimeSubmission_DoesNotProbePaymentService_AndNotifiesPaymentService()
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
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test", IsResubmission = false },
        });

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()), Times.Never);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService)), Times.Once);
    }

    [Test]
    public async Task Post_Resubmission_WithPaymentServiceSnapshot_NotifiesPaymentService()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test", IsResubmission = true },
        });
        _paymentCalculationServiceMock
            .Setup(x => x.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(submission.Id), Times.Once);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService)), Times.Once);
    }

    [Test]
    public async Task Post_Resubmission_WithoutPaymentServiceSnapshot_SuppressesPaymentServiceNotification()
    {
        // Arrange - IsSubmitted=true (already-submitted, i.e. a genuine resubmit of a legacy Synapse-only
        // submission id) AND the payment service does not know the submission (404) - so we suppress.
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test", IsResubmission = true },
        });
        _paymentCalculationServiceMock
            .Setup(x => x.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(submission.Id), Times.Once);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => !c.NotifyPaymentService)), Times.Once);
    }

    [Test]
    public async Task Post_Resubmission_FirstSubmitOfNewId_NotifiesPaymentServiceWithoutProbing()
    {
        // Arrange - IsResubmission=true (e.g. after a regulator-cancelled prior submission), but the current
        // submission id has never been submitted before (IsSubmitted=false). This is a first-time submit of
        // a fresh id and the payment service must be notified. The fee-params probe must NOT be called.
        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "FileName",
                CompanyDetailsUploadDatetime = DateTime.Now,
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsFileId = Guid.NewGuid(),
            },
            HasValidFile = true,
        };
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test", IsResubmission = true },
        });

        var request = new DeclarationWithFullNameViewModel
        {
            FullName = DeclarationName,
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
        };
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, request);

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()), Times.Never);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService)), Times.Once);
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public async Task Post_AfterPreviousApplicationSubmission_NotResubmission_NotifiesOnlyWhenKnownToPaymentService(
        bool knownToPaymentService,
        bool expectedNotify)
    {
        // Arrange - a declaration following a regulator query: IsResubmission stays false, but an application was
        // already submitted, so the payment service must already hold the submission's history.
        var submission = CreateSubmittedRegistrationSubmission();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: false, hasPreviousApplicationSubmission: true);
        SetupRegistrationSession(isResubmission: false);
        _paymentCalculationServiceMock
            .Setup(x => x.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(knownToPaymentService);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, CreateDeclarationRequest());

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(submission.Id), Times.Once);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService == expectedNotify)), Times.Once);
    }

    [Test]
    public async Task Post_AlreadySubmitted_NoPreviousApplicationSubmission_NotResubmission_NotifiesWithoutProbing()
    {
        // Arrange - a file re-declared before any application was submitted: no lateness history can be lost.
        var submission = CreateSubmittedRegistrationSubmission();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: false, hasPreviousApplicationSubmission: false);
        SetupRegistrationSession(isResubmission: false);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, CreateDeclarationRequest());

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()), Times.Never);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService)), Times.Once);
    }

    [Test]
    public async Task Post_FirstSubmitOfId_WithPreviousApplicationSubmission_NotifiesWithoutProbing()
    {
        // Arrange - IsSubmitted=false: the submission id has never been declared, so it is always notified.
        var submission = CreateSubmittedRegistrationSubmission();
        submission.IsSubmitted = false;
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: false, hasPreviousApplicationSubmission: true);
        SetupRegistrationSession(isResubmission: false);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        await _systemUnderTest.Post(submission.Id, CreateDeclarationRequest());

        // Assert
        _paymentCalculationServiceMock.Verify(s => s.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()), Times.Never);
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.Is<RegistrationSubmitContext>(c => c.NotifyPaymentService)), Times.Once);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Post_PaymentServiceLookupFails_DoesNotSubmitAndShowsSubmissionFailed(bool isResubmission, bool hasPreviousApplicationSubmission)
    {
        // Arrange - an outage must not be mistaken for "not known" and silently move the submission to the legacy path.
        var submission = CreateSubmittedRegistrationSubmission();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);
        SetupSubmitDetails(isResubmission: isResubmission, hasPreviousApplicationSubmission: hasPreviousApplicationSubmission);
        SetupRegistrationSession(isResubmission: isResubmission);
        _paymentCalculationServiceMock
            .Setup(x => x.IsSubmissionKnownToPaymentServiceAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new HttpRequestException("payment service unavailable"));
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer));

        // Act
        var result = await _systemUnderTest.Post(submission.Id, CreateDeclarationRequest()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("OrganisationDetailsSubmissionFailed");
        _submissionServiceMock.Verify(x => x.SubmitAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<RegistrationJourney?>(),
            It.IsAny<RegistrationSubmitContext?>()), Times.Never);
    }

    private static RegistrationSubmission CreateSubmittedRegistrationSubmission() => new()
    {
        Id = Guid.NewGuid(),
        IsSubmitted = true,
        LastUploadedValidFiles = new UploadedRegistrationFilesInformation
        {
            CompanyDetailsFileName = "FileName",
            CompanyDetailsUploadDatetime = DateTime.Now,
            CompanyDetailsUploadedBy = Guid.NewGuid(),
            CompanyDetailsFileId = Guid.NewGuid(),
        },
        HasValidFile = true,
    };

    private void SetupRegistrationSession(bool isResubmission) =>
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { ApplicationReferenceNumber = "test", IsResubmission = isResubmission },
        });

    private static DeclarationWithFullNameViewModel CreateDeclarationRequest() => new()
    {
        FullName = DeclarationName,
        OrganisationDetailsFileId = Guid.NewGuid().ToString(),
    };
}
