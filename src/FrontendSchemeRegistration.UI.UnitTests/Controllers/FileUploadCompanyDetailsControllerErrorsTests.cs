namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadCompanyDetailsErrorsControllerTests
{
    private const string ContentType = "text/csv";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private static readonly DateTime SubmissionDeadline = DateTime.UtcNow.Date;
    private static readonly int RowErrorCount = 5;
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private readonly NullLogger<FileUploadCompanyDetailsErrorsController> _nullLogger = new();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadCompanyDetailsErrorsController _systemUnderTest;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;
    private Mock<IUrlHelper> _urlHelper;

    [SetUp]
    public void SetUp()
    {
        _urlHelper = new Mock<IUrlHelper>();
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
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
                    SubmissionPeriod = SubmissionPeriod,
                    SubmissionDeadline = SubmissionDeadline,
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                }
            });

        _systemUnderTest = new FileUploadCompanyDetailsErrorsController(_submissionServiceMock.Object, _sessionManagerMock.Object, _nullLogger, _registrationApplicationServiceMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        {
                            "SubmissionId", SubmissionId.ToString()
                        }
                    }),
                    ContentType = ContentType
                },
                Session = new Mock<ISession>().Object
            }
        };
        _systemUnderTest.Url = _urlHelper.Object;
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenSessionIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(null as FrontendSchemeRegistrationSession);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenOrganisationRoleIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>()
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenJourneyIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    Journey = { },
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenSubmissionIdInQuery()
    {
        await _systemUnderTest.Get();

        // Assert
        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsErrorsView_WhenSubmissionIsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(null as RegistrationSubmission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenSubmissionIsValid()
    {
        // Arrange
        _urlHelper.Setup(x => x.Content("~//report-organisation-details")).Returns("/someurl");
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                RowErrorCount = 5,
                RegistrationJourney = RegistrationJourney.CsoLargeProducer
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.Model.Should().BeEquivalentTo(new FileUploadErrorsViewModel
        {
            SubmissionDeadline = SubmissionDeadline,
            OrganisationRole = OrganisationRoles.Producer,
            ErrorCount = RowErrorCount,
            SubmissionId = SubmissionId,
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
            
        });
        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }
}