namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.DTOs.UserAccount;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Extensions;
using UI.Sessions;
using UI.ViewModels;
using Organisation = EPR.Common.Authorization.Models.Organisation;

[TestFixture]
public class CompanyDetailsConfirmationControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private CompanyDetailsConfirmationController _systemUnderTest;
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;


    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUploadCompanyDetailsSubLanding);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _userAccountServiceMock = new Mock<IUserAccountService>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
                },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    }
                }
            });
        _registrationApplicationServiceMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        _systemUnderTest = new CompanyDetailsConfirmationController(_submissionServiceMock.Object, _sessionManagerMock.Object, _userAccountServiceMock.Object, _registrationApplicationServiceMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", SubmissionId.ToString() }
                    })
                },
                Session = new Mock<ISession>().Object
            }
        };

        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_ReturnsFileUploadSuccessView_WhenCalled()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();

        DateTime submissionTime = DateTime.UtcNow;

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            IsSubmitted = true,
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                SubmittedDateTime = submissionTime,
                SubmittedBy = Guid.NewGuid()
            },
            RegistrationJourney = RegistrationJourney.CsoLargeProducer,
        });

        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("CompanyDetailsConfirmation");
        result.ViewData.Keys.Should().HaveCount(2);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData.Keys.Should().Contain("IsFileUploadJourneyInvokedViaRegistration");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.FileUploadCompanyDetailsSubLanding}?registrationyear={submissionTime.Year}&registrationjourney={RegistrationJourney.CsoLargeProducer}");
        result.ViewData["IsFileUploadJourneyInvokedViaRegistration"].Should().Be(false);
        result.Model.Should().BeEquivalentTo(new CompanyDetailsConfirmationModel
        {
            SubmissionTime = submissionTime.ToTimeHoursMinutes(),
            SubmittedDate = submissionTime.ToReadableDate(),
            SubmittedBy = fullName,
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            RegistrationYear = submissionTime.Year,
            RegistrationJourney = RegistrationJourney.CsoLargeProducer,
        });
    }

    [Test]
    public async Task Get_ReturnsFileUploadSuccessView_WhenCalled_From_RegistrationTaskList()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new Organisation { OrganisationRole = OrganisationRoles.ComplianceScheme } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    IsFileUploadJourneyInvokedViaRegistration = true
                }
            });

        DateTime submissionTime = DateTime.UtcNow;

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            IsSubmitted = true,
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                SubmittedDateTime = submissionTime,
                SubmittedBy = Guid.NewGuid()
            }
        });

        const string firstName = "first";
        const string lastName = "last";
        const string fullName = $"{firstName} {lastName}";

        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("CompanyDetailsConfirmation");
        result.ViewData.Keys.Should().HaveCount(2);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData.Keys.Should().Contain("IsFileUploadJourneyInvokedViaRegistration");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.RegistrationTaskList}?registrationyear={submissionTime.Year}");
        result.ViewData["IsFileUploadJourneyInvokedViaRegistration"].Should().Be(true);
        result.Model.Should().BeEquivalentTo(new CompanyDetailsConfirmationModel
        {
            SubmissionTime = submissionTime.ToTimeHoursMinutes(),
            SubmittedDate = submissionTime.ToReadableDate(),
            SubmittedBy = fullName,
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            RegistrationYear = submissionTime.Year,
        });
    }

    [Test]
    public async Task Get_RedirectsToLandingPage_WhenSessionIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    [Test]
    public async Task Get_RedirectsToLandingPage_WhenSubmissionIsNotCompleted()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();

        DateTime submissionTime = DateTime.Now;

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            Id = SubmissionId,
            IsSubmitted = false,
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                SubmittedDateTime = submissionTime,
                SubmittedBy = Guid.NewGuid()
            }
        });

        const string firstName = "first";
        const string lastName = "last";

        _userAccountServiceMock.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new PersonDto
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("Landing");
    }

    private void SetupSessionManagerMockWithComplianceScheme()
    {
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new Organisation { OrganisationRole = OrganisationRoles.ComplianceScheme } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships,
                    },
                    IsFileUploadJourneyInvokedViaRegistration = false
                }
            });
    }
}