namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Application.DTOs.Submission;
using Application.DTOs.UserAccount;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Organisation = EPR.Common.Authorization.Models.Organisation;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Services;

[TestFixture]
public class FileReUploadCompanyDetailsConfirmationControllerTests
{
    private static readonly DateTime SubmissionDeadline = DateTime.UtcNow;
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private FileReUploadCompanyDetailsConfirmationController _systemUnderTest;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;


    [SetUp]
    public void SetUp()
    {
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = SubmissionDeadline
                },
                UserData = new UserData
                {
                    ServiceRole = "Basic User",
                    Organisations = new List<Organisation>
                    {
                        new Organisation
                        {
                            OrganisationRole = "Producer"
                        }
                    }
                }
            });

        _submissionServiceMock = new Mock<ISubmissionService>();
        _userAccountServiceMock = new Mock<IUserAccountService>();

        _systemUnderTest =
            new FileReUploadCompanyDetailsConfirmationController(
                _submissionServiceMock.Object,
                _userAccountServiceMock.Object, _sessionManagerMock.Object, _registrationApplicationServiceMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        {
                            "submissionId", SubmissionId.ToString()
                        }
                    })
                },
                Session = new Mock<ISession>().Object
            }
        };
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _systemUnderTest.Url = urlHelperMock.Object;
    }

    [Test]
    public async Task Get_RedirectsToSubLanding_WhenSubmissionIsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync((RegistrationSubmission)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsSubLandingController.Get));
        result.ControllerName.Should()
            .Be(nameof(FileUploadCompanyDetailsSubLandingController).RemoveControllerFromName());
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenCalled()
    {
        // Arrange
        const string orgDetailsFileName = "filename.csv";
        DateTime orgDetailsUploadDate = DateTime.UtcNow;
        Guid orgDetailsUploadedBy = Guid.NewGuid();

        const string partnersFileName = "partners.csv";
        DateTime partnersUploadDate = DateTime.UtcNow;
        Guid partnersUploadedBy = Guid.NewGuid();

        const string brandFileName = "brand.csv";
        DateTime brandUploadDate = DateTime.UtcNow;
        Guid brandUploadedBy = Guid.NewGuid();

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsUploadedDate = orgDetailsUploadDate,
            CompanyDetailsUploadedBy = orgDetailsUploadedBy,
            PartnershipsFileName = partnersFileName,
            PartnershipsUploadedDate = partnersUploadDate,
            PartnershipsUploadedBy = partnersUploadedBy,
            BrandsFileName = brandFileName,
            BrandsUploadedDate = brandUploadDate,
            BrandsUploadedBy = brandUploadedBy,
            HasValidFile = true,
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = orgDetailsFileName,
                BrandsFileName = brandFileName,
                PartnershipsFileName = partnersFileName,
                CompanyDetailsFileId = default(Guid),
                CompanyDetailsUploadedBy = orgDetailsUploadedBy,
                CompanyDetailsUploadDatetime = orgDetailsUploadDate,
                BrandsUploadedBy = brandUploadedBy,
                BrandsUploadDatetime = brandUploadDate,
                PartnershipsUploadedBy = partnersUploadedBy,
                PartnershipsUploadDatetime = partnersUploadDate
            }
        });
        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileReUploadCompanyDetailsConfirmation");
        result.Model.Should().BeEquivalentTo(new FileReUploadCompanyDetailsConfirmationViewModel
        {
            SubmissionId = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsFileUploadDate = orgDetailsUploadDate.ToReadableDate(),
            CompanyDetailsFileUploadedBy = fullName,
            BrandsFileName = brandFileName,
            BrandsFileUploadDate = brandUploadDate.ToReadableDate(),
            BrandsFileUploadedBy = fullName,
            PartnersFileName = partnersFileName,
            PartnersFileUploadDate = partnersUploadDate.ToReadableDate(),
            PartnersFileUploadedBy = fullName,
            SubmissionDeadline = SubmissionDeadline.ToReadableDate(),
            IsSubmitted = false,
            HasValidfile = true,
            Status = SubmissionPeriodStatus.FileUploaded,
            OrganisationRole = "Producer"
        });

        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(It.IsAny<Guid>()), Times.AtLeastOnce);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenCalled_FromRegistrationTaskList()
    {
        // Arrange
        const string orgDetailsFileName = "filename.csv";
        DateTime orgDetailsUploadDate = DateTime.UtcNow;
        Guid orgDetailsUploadedBy = Guid.NewGuid();

        const string partnersFileName = "partners.csv";
        DateTime partnersUploadDate = DateTime.UtcNow;
        Guid partnersUploadedBy = Guid.NewGuid();

        const string brandFileName = "brand.csv";
        DateTime brandUploadDate = DateTime.UtcNow;
        Guid brandUploadedBy = Guid.NewGuid();

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsUploadedDate = orgDetailsUploadDate,
            CompanyDetailsUploadedBy = orgDetailsUploadedBy,
            PartnershipsFileName = partnersFileName,
            PartnershipsUploadedDate = partnersUploadDate,
            PartnershipsUploadedBy = partnersUploadedBy,
            BrandsFileName = brandFileName,
            BrandsUploadedDate = brandUploadDate,
            BrandsUploadedBy = brandUploadedBy,
            HasValidFile = true,
            IsSubmitted = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = orgDetailsFileName,
                BrandsFileName = brandFileName,
                PartnershipsFileName = partnersFileName,
                CompanyDetailsFileId = default(Guid),
                CompanyDetailsUploadedBy = orgDetailsUploadedBy,
                CompanyDetailsUploadDatetime = orgDetailsUploadDate,
                BrandsUploadedBy = brandUploadedBy,
                BrandsUploadDatetime = brandUploadDate,
                PartnershipsUploadedBy = partnersUploadedBy,
                PartnershipsUploadDatetime = partnersUploadDate
            }
        });
        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = SubmissionDeadline,
                    IsFileUploadJourneyInvokedViaRegistration = true
                },
                UserData = new UserData
                {
                    ServiceRole = "Basic User",
                    Organisations = new List<Organisation>
                    {
                        new Organisation
                        {
                            OrganisationRole = "Producer"
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileReUploadCompanyDetailsConfirmation");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData.Keys.Should().Contain("IsFileUploadJourneyInvokedViaRegistration");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.RegistrationTaskList}");
        result.Model.Should().BeEquivalentTo(new FileReUploadCompanyDetailsConfirmationViewModel
        {
            SubmissionId = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsFileUploadDate = orgDetailsUploadDate.ToReadableDate(),
            CompanyDetailsFileUploadedBy = fullName,
            BrandsFileName = brandFileName,
            BrandsFileUploadDate = brandUploadDate.ToReadableDate(),
            BrandsFileUploadedBy = fullName,
            PartnersFileName = partnersFileName,
            PartnersFileUploadDate = partnersUploadDate.ToReadableDate(),
            PartnersFileUploadedBy = fullName,
            SubmissionDeadline = SubmissionDeadline.ToReadableDate(),
            IsSubmitted = false,
            HasValidfile = true,
            Status = SubmissionPeriodStatus.FileUploaded,
            OrganisationRole = "Producer"
        });
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenCalledWithSubmittedFile()
    {
        // Arrange
        const string orgDetailsFileName = "filename.csv";
        DateTime orgDetailsUploadDate = DateTime.UtcNow;
        Guid orgDetailsUploadedBy = Guid.NewGuid();

        const string partnersFileName = "partners.csv";
        DateTime partnersUploadDate = DateTime.UtcNow;
        Guid partnersUploadedBy = Guid.NewGuid();

        const string brandFileName = "brand.csv";
        DateTime brandUploadDate = DateTime.UtcNow;
        Guid brandUploadedBy = Guid.NewGuid();

        var submitDate = orgDetailsUploadDate.AddHours(1);

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsUploadedDate = orgDetailsUploadDate,
            CompanyDetailsUploadedBy = orgDetailsUploadedBy,
            PartnershipsFileName = partnersFileName,
            PartnershipsUploadedDate = partnersUploadDate,
            PartnershipsUploadedBy = partnersUploadedBy,
            BrandsFileName = brandFileName,
            BrandsUploadedDate = brandUploadDate,
            BrandsUploadedBy = brandUploadedBy,
            HasValidFile = true,
            IsSubmitted = true,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileName = orgDetailsFileName,
                BrandsFileName = brandFileName,
                PartnershipsFileName = partnersFileName,
                CompanyDetailsFileId = default(Guid),
                CompanyDetailsUploadedBy = orgDetailsUploadedBy,
                CompanyDetailsUploadDatetime = orgDetailsUploadDate,
                BrandsUploadedBy = brandUploadedBy,
                BrandsUploadDatetime = brandUploadDate,
                PartnershipsUploadedBy = partnersUploadedBy,
                PartnershipsUploadDatetime = partnersUploadDate
            },
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = orgDetailsFileName,
                BrandsFileName = brandFileName,
                PartnersFileName = partnersFileName,
                SubmittedBy = Guid.NewGuid(),
                SubmittedDateTime = submitDate
            }
        });
        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileReUploadCompanyDetailsConfirmation");
        result.Model.Should().BeEquivalentTo(new FileReUploadCompanyDetailsConfirmationViewModel
        {
            SubmissionId = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsFileUploadDate = submitDate.ToReadableDate(),
            CompanyDetailsFileUploadedBy = fullName,
            BrandsFileName = brandFileName,
            BrandsFileUploadDate = submitDate.ToReadableDate(),
            BrandsFileUploadedBy = fullName,
            PartnersFileName = partnersFileName,
            PartnersFileUploadDate = submitDate.ToReadableDate(),
            PartnersFileUploadedBy = fullName,
            SubmissionDeadline = SubmissionDeadline.ToReadableDate(),
            IsSubmitted = true,
            HasValidfile = true,
            Status = SubmissionPeriodStatus.SubmittedToRegulator,
            OrganisationRole = "Producer"
        });

        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsCorrectViewAndModel_WhenCalledWithUploadedCompanyFile()
    {
        // Arrange
        const string orgDetailsFileName = "filename.csv";
        DateTime orgDetailsUploadDate = DateTime.UtcNow;
        Guid orgDetailsUploadedBy = Guid.NewGuid();

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsUploadedDate = orgDetailsUploadDate,
            CompanyDetailsUploadedBy = orgDetailsUploadedBy,
            HasValidFile = true,
        });
        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetAllPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileReUploadCompanyDetailsConfirmation");
        result.Model.Should().BeEquivalentTo(new FileReUploadCompanyDetailsConfirmationViewModel
        {
            SubmissionId = SubmissionId,
            CompanyDetailsFileName = orgDetailsFileName,
            CompanyDetailsFileUploadDate = orgDetailsUploadDate.ToReadableDate(),
            CompanyDetailsFileUploadedBy = fullName,
            SubmissionDeadline = SubmissionDeadline.ToReadableDate(),
            HasValidfile = true,
            Status = SubmissionPeriodStatus.NotStarted,
            OrganisationRole = "Producer"
        });

        _userAccountServiceMock.Verify(x => x.GetAllPersonByUserId(It.IsAny<Guid>()), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(It.IsAny<Guid>()), Times.Never);
    }
}