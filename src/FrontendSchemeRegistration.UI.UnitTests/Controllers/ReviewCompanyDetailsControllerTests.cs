using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.DTOs.UserAccount;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Organisation = EPR.Common.Authorization.Models.Organisation;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Enums;
using Organisation = Organisation;

public class ReviewCompanyDetailsControllerTests
{
    private static readonly string SubmissionId = Guid.NewGuid().ToString();
    private static readonly DateTime SubmissionDeadline = DateTime.UtcNow.Date;
    private ReviewCompanyDetailsController _systemUnderTest;
    private Mock<ISubmissionService> _submissionService;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUploadCompanyDetailsSubLanding);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _submissionService = new Mock<ISubmissionService>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _sessionManagerMock = new();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = SubmissionDeadline,
                },
                UserData = new UserData { Organisations = { new() { OrganisationRole = OrganisationRoles.ComplianceScheme } }, ServiceRole = ServiceRoles.ApprovedPerson }
            });
        _registrationApplicationServiceMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        _systemUnderTest = new ReviewCompanyDetailsController(
            _submissionService.Object,
            _userAccountServiceMock.Object,
            _sessionManagerMock.Object,
            new NullLogger<ReviewCompanyDetailsController>(), _registrationApplicationServiceMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object,
                User = _claimsPrincipalMock.Object
            }
        };

        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved, false, RegistrationJourney.CsoLargeProducer)]
    public async Task Get_ReturnsCorrectView(string serviceRole, string enrolmentStatus, bool expectedIsApprovedUser, RegistrationJourney? expectedRegistrationJourney = null)
    {
        // Arrange
        const string firstName = "First Name";
        const string lastName = "Last Name";
        var submission = GenerateRegistrationSubmission(expectedRegistrationJourney);
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(submission.Id))
            .ReturnsAsync(submission);
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(new PersonDto
            {
                FirstName = firstName,
                LastName = lastName,
                ContactEmail = "email@email.com"
            });
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get(submission.Id) as ViewResult;

        // Assert
        result.ViewName.Should().Be("ReviewCompanyDetails");
        result.ViewData.Keys.Should().HaveCount(2);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData.Keys.Should().Contain("IsFileUploadJourneyInvokedViaRegistration");
        if (expectedRegistrationJourney != null)
        {
            result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.FileUploadCompanyDetailsSubLanding}?registrationyear={DateTime.Now.Year}&registrationjourney={expectedRegistrationJourney.ToString()}");
        }
        else
        {
            result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.FileUploadCompanyDetailsSubLanding}?registrationyear={DateTime.Now.Year}");    
        }
        
        result.ViewData["IsFileUploadJourneyInvokedViaRegistration"].Should().Be(false);
        var model = result.Model.As<ReviewCompanyDetailsViewModel>();
        model.OrganisationDetailsUploadedBy.Should().BeEquivalentTo($"{firstName} {lastName}");
        model.RegistrationSubmissionDeadline.Should().BeEquivalentTo(SubmissionDeadline.ToReadableDate());
        model.IsApprovedUser.Should().Be(expectedIsApprovedUser);
        model.RegistrationJourney.Should().Be(expectedRegistrationJourney);
    }

    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Approved, true)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.DelegatedPerson, EnrolmentStatuses.Rejected, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Approved, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Invited, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Pending, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.NotSet, false)]
    [TestCase(ServiceRoles.BasicUser, EnrolmentStatuses.Rejected, false)]
    public async Task Get_ReturnsCorrectView_WhenCalled_FromRegistrationTaskList(string serviceRole, string enrolmentStatus, bool expectedIsApprovedUser)
    {
        // Arrange
        const string firstName = "First Name";
        const string lastName = "Last Name";
        var submission = GenerateRegistrationSubmission();
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(submission.Id))
            .ReturnsAsync(submission);
        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>()))
            .ReturnsAsync(new PersonDto
            {
                FirstName = firstName,
                LastName = lastName,
                ContactEmail = "email@email.com"
            });
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = SubmissionDeadline,
                    IsFileUploadJourneyInvokedViaRegistration = true
                },
                UserData = new UserData { Organisations = { new() { OrganisationRole = OrganisationRoles.ComplianceScheme } }, ServiceRole = ServiceRoles.ApprovedPerson }
            });

        // Act
        var result = await _systemUnderTest.Get(submission.Id) as ViewResult;

        // Assert
        result.ViewName.Should().Be("ReviewCompanyDetails");
        result.ViewData.Keys.Should().HaveCount(2);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData.Keys.Should().Contain("IsFileUploadJourneyInvokedViaRegistration");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.RegistrationTaskList}?registrationyear={DateTime.Now.Year}");
        result.ViewData["IsFileUploadJourneyInvokedViaRegistration"].Should().Be(true);
        var model = result.Model.As<ReviewCompanyDetailsViewModel>();
        model.OrganisationDetailsUploadedBy.Should().BeEquivalentTo($"{firstName} {lastName}");
        model.RegistrationSubmissionDeadline.Should().BeEquivalentTo(SubmissionDeadline.ToReadableDate());
        model.IsApprovedUser.Should().Be(expectedIsApprovedUser);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenSubmissionIsNull()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync((RegistrationSubmission) null);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get(Guid.NewGuid()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenHasInvalidFile()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = submissionId,
                HasValidFile = false
            });

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenHasInvalidFile()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = false
            });

        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.Producer, true, true);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLanding_WhenValidationPassFalse()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                ValidationPass = false
            });

        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.Producer, true, true);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLanding_WhenSubmitOrganisationDetailsResponseIsFalse()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.Producer, true, false);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_RedirectsToDeclarationWithFullName_WhenDirectProducerSubmits()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.Producer, true, true);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result?.ControllerName.Should().Be("DeclarationWithFullName");
    }

    [Test]
    public async Task Post_RedirectsToDeclarationWithFullName_WhenComplianceSchemeSubmits()
    {
        // Arrange
        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.ComplianceScheme, true, true);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result?.ControllerName.Should().Be("DeclarationWithFullName");
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
    public async Task Post_RedirectsToGet_WhenUserDoesNotHavePermissionToSubmit(string serviceRole, string enrolmentStatus)
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel();
        var claims = CreateUserDataClaim(serviceRole, enrolmentStatus, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result!.ActionName.Should().Be("Get");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadCompanyDetailsSubLanding_WhenUserDoesNotSubmit()
    {
        // Arrange
        var model = GenerateReviewCompanyDetailsModel(OrganisationRoles.ComplianceScheme, true, false);

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result?.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel();

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _systemUnderTest.ModelState.AddModelError("Key", "Value");

        // Act
        var result = await _systemUnderTest.Post(model) as ViewResult;

        // Assert
        result.ViewName.Should().Be("ReviewCompanyDetails");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenModelIsApprovedUserIsFalse()
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel
        {
            IsApprovedUser = false
        };

        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as UnauthorizedResult;

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenSubmitOrganisationDetailsResponseFalseAndIsFileUploadJourneyInvokedViaRegistrationAndRegistrationYearIsNull()
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel
        {
            IsApprovedUser = true,
            SubmitOrganisationDetailsResponse = false
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    IsFileUploadJourneyInvokedViaRegistration = true
                }
            });

        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectResult;

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().NotContain("registrationyear");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenSubmitOrganisationDetailsResponseFalseAndIsFileUploadJourneyInvokedViaRegistrationAndRegistrationYearIs2025()
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel
        {
            IsApprovedUser = true,
            SubmitOrganisationDetailsResponse = false,
            RegistrationYear = 2025
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    IsFileUploadJourneyInvokedViaRegistration = true
                }
            });

        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectResult;

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("registrationyear");
    }

    [Test]
    public async Task Post_ReturnsCorrectView_WhenSubmitOrganisationDetailsResponseFalseAndIsFileUploadJourneyInvokedViaRegistrationIsFalse()
    {
        // Arrange
        var model = new ReviewCompanyDetailsViewModel
        {
            IsApprovedUser = true,
            SubmitOrganisationDetailsResponse = false,
            RegistrationYear = 2025
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    IsFileUploadJourneyInvokedViaRegistration = false
                }
            });

        _submissionService
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                HasValidFile = true
            });

        var claims = CreateUserDataClaim(ServiceRoles.ApprovedPerson, EnrolmentStatuses.Approved, OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Post(model) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");
    }

    private static ReviewCompanyDetailsViewModel GenerateReviewCompanyDetailsModel(string role, bool isApprovedUser, bool? submitOrgDetails)
    {
        return new ReviewCompanyDetailsViewModel
        {
            OrganisationRole = role,
            SubmissionId = Guid.NewGuid(),
            OrganisationDetailsFileName = "Organisation Details File Name.csv",
            OrganisationDetailsUploadedBy = "User",
            OrganisationDetailsFileUploadDate = DateTime.UtcNow.ToReadableDate(),
            OrganisationDetailsFileId = Guid.NewGuid().ToString(),
            BrandFileName = "Brand File Name.csv",
            BrandUploadedBy = "null",
            BrandFileUploadDate = DateTime.UtcNow.ToReadableDate(),
            PartnerFileName = "Partner File Name.csv",
            PartnerUploadedBy = "null",
            PartnerFileUploadDate = DateTime.UtcNow.ToReadableDate(),
            RegistrationSubmissionDeadline = DateTime.UtcNow.AddDays(1).ToReadableDate(),
            BrandsRequired = true,
            PartnersRequired = true,
            IsApprovedUser = isApprovedUser,
            HasPreviousSubmission = false,
            SubmittedCompanyDetailsFileName = "SubmittedCompanyDetailsFileName",
            SubmittedCompanyDetailsDateTime = "SubmittedCompanyDetailsDateTime",
            SubmittedBrandsFileName = "SubmittedBrandsFileName",
            SubmittedBrandsDateTime = "SubmittedBrandsDateTime",
            SubmittedPartnersFileName = "SubmittedPartnersFileName",
            SubmittedPartnersDateTime = "SubmittedPartnersDateTime",
            SubmittedDateTime = "SubmittedDateTime",
            SubmittedBy = "SubmittedBy",
            SubmitOrganisationDetailsResponse = submitOrgDetails,
            RegistrationYear = DateTime.Now.Year
        };
    }

    private static RegistrationSubmission GenerateRegistrationSubmission(RegistrationJourney? registrationJourney = null)
    {
        return new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            SubmissionPeriod = "PaymentMethod",
            ValidationPass = true,
            HasValidFile = true,
            Errors = new List<string>(),
            IsSubmitted = false,
            BrandsDataComplete = true,
            BrandsFileName = "BrandsFileName",
            BrandsUploadedBy = Guid.NewGuid(),
            BrandsUploadedDate = DateTime.UtcNow,
            CompanyDetailsFileName = "CompanyDetailsFileName",
            CompanyDetailsUploadedBy = Guid.NewGuid(),
            CompanyDetailsUploadedDate = DateTime.UtcNow,
            CompanyDetailsDataComplete = false,
            PartnershipsDataComplete = true,
            PartnershipsFileName = "PartnershipsFileName",
            PartnershipsUploadedBy = Guid.NewGuid(),
            PartnershipsUploadedDate = DateTime.UtcNow,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "CompanyDetailsFileName",
                BrandsFileName = "BrandsFileName",
                PartnersFileName = "PartnersFileName",
                SubmittedDateTime = DateTime.UtcNow,
                SubmittedBy = Guid.NewGuid()
            },
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = "CompanyDetailsFileName",
                BrandsFileName = "BrandsFileName",
                PartnershipsFileName = "PartnersFileName",
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsUploadedBy = Guid.NewGuid(),
                CompanyDetailsUploadDatetime = DateTime.UtcNow,
                BrandsUploadedBy = Guid.NewGuid(),
                BrandsUploadDatetime = DateTime.UtcNow,
                PartnershipsUploadedBy = Guid.NewGuid(),
                PartnershipsUploadDatetime = null
            },
            RegistrationJourney = registrationJourney
        };
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
                    Name = "Some org"
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}