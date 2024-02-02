namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Services.Interfaces;
using UI.Sessions;

[TestFixture]
public class FileUploadCompanyDetailsErrorsControllerTests
{
    private const string ContentType = "text/csv";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private readonly NullLogger<FileUploadCompanyDetailsErrorsController> _nullLogger = new();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadCompanyDetailsErrorsController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
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
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                }
            });

        new Mock<IFileUploadService>();

        _systemUnderTest = new FileUploadCompanyDetailsErrorsController(_submissionServiceMock.Object, _sessionManagerMock.Object, _nullLogger);
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
                    }),
                    ContentType = ContentType
                },
                Session = new Mock<ISession>().Object
            }
        };
        _systemUnderTest.Url = Mock.Of<IUrlHelper>();
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsLandingPageView_WhenSessionIsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = Guid.NewGuid()
            });
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
        var submissionDeadline = DateTime.UtcNow.Date;
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = Guid.NewGuid()
            });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }
}