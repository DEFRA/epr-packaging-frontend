namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Services.Interfaces;
using UI.Sessions;

[TestFixture]
public class FileUploadBrandsControllerTests
{
    private const string ContentType = "text/csv";
    private readonly Guid _registrationSetId = Guid.NewGuid();
    private readonly string _submissionPeriod = "subPeriod";
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFileUploadService> _fileUploadServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadBrandsController _systemUnderTest;
    private Mock<IRegistrationPeriodProvider> _registrationPeriodProviderMock;
    private Mock<ISessionManager<RegistrationApplicationSession>> _registrationApplicationSessionManagerMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _registrationApplicationSessionManagerMock = new Mock<ISessionManager<RegistrationApplicationSession>>();

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
                    },
                    LatestRegistrationSet = new Dictionary<string, Guid>
                    {
                        [_submissionPeriod] = _registrationSetId
                    },
                    SubmissionPeriod = _submissionPeriod
                }
            });
        _fileUploadServiceMock = new Mock<IFileUploadService>();
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        _systemUnderTest = new FileUploadBrandsController(
            _submissionServiceMock.Object, 
            _fileUploadServiceMock.Object, 
            _sessionManagerMock.Object,
            _registrationApplicationSessionManagerMock.Object,
            Options.Create(new GlobalVariables { FileUploadLimitInBytes = 268435456, SubsidiaryFileUploadLimitInBytes = 61440 }),
            _registrationPeriodProviderMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(),
                    ContentType = ContentType
                },
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_ReturnsFileUploadBrandsView_WhenRequiresBrandsFile()
    {
        // Arrange
        const string contentType = "content-type";
        var submissionId = Guid.NewGuid();

        _systemUnderTest.ControllerContext.HttpContext.Request.ContentType = contentType;
        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", submissionId.ToString() } });

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { RequiresBrandsFile = true });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new() { OrganisationRole = OrganisationRoles.ComplianceScheme } } },
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

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result?.ViewName.Should().Be("FileUploadBrands");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadingCompanyDetails_WhenTheModelStateIsValid()
    {
        // Arrange
        const string contentType = "content-type";
        var submissionId = Guid.NewGuid();
        _systemUnderTest.ControllerContext.HttpContext.Request.ContentType = contentType;
        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", submissionId.ToString() } });

        _fileUploadServiceMock
            .Setup(x => x.ProcessUploadAsync(
                contentType,
                It.IsAny<Stream>(),
                It.IsAny<ModelStateDictionary>(),
                It.IsAny<IFileUploadSize>(),
                It.IsAny<FileUploadSubmissionDetails>()))
            .ReturnsAsync(submissionId);

        // Act
        var result = await _systemUnderTest.Post(It.IsAny<string>()) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Contain("FileUploadingBrands");
        result.RouteValues.Should().Contain(new KeyValuePair<string, object?>("submissionId", submissionId));
    }

    [Test]
    public async Task Post_ReturnsFileUploadCompanyDetailsView_WhenTheModelStateIsInvalid()
    {
        // Arrange
        const string contentType = "content-type";
        var submissionId = Guid.NewGuid();
        _systemUnderTest.ControllerContext.HttpContext.Request.ContentType = contentType;
        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", submissionId.ToString() } });

        _fileUploadServiceMock
            .Setup(x => x.ProcessUploadAsync(
                contentType,
                It.IsAny<Stream>(),
                It.IsAny<ModelStateDictionary>(),
                It.IsAny<IFileUploadSize>(),
                It.IsAny<FileUploadSubmissionDetails>()))
            .ReturnsAsync(submissionId);
        _systemUnderTest.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _systemUnderTest.Post(It.IsAny<string>()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadBrands");
    }

    [Test]
    public async Task Get_RedirectsToFileUploadBrands_WhenOrganisationRolesIsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { RequiresBrandsFile = true });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new() { OrganisationRole = null } } },
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

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result?.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_CallsSubmissionService_WhenOrganisationRoleIsNotNull()
    {
        // Arrange
        _systemUnderTest.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", Guid.NewGuid().ToString() }
        });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new() { OrganisationRole = "Admin" } } },
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

        // Act
        var result = await _systemUnderTest.Get();

        // Assert
        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadBrands_WhenSubmissionIsNull()
    {
        // Arrange
        _systemUnderTest.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", Guid.NewGuid().ToString() }
        });
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync((RegistrationSubmission?)null);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new() { OrganisationRole = OrganisationRoles.Producer } } }
            });
        // Act
        var result = await _systemUnderTest.Get();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetails_WhenSessionIsNull()
    {
        // Arrange
        _systemUnderTest.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", Guid.NewGuid().ToString() }
        });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession?)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result?.ControllerName.Should().Be("FileUploadCompanyDetails");
    }

    [Test]
    public async Task Get_SetsBackLinkToOrganisationDetailsUploaded_WhenRequiresBrandsFile()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var registrationYear = DateTime.Now.Year;

        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues>
            {
                { "submissionId", submissionId.ToString() },
                { "registrationyear", registrationYear.ToString() }
            });

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { RequiresBrandsFile = true });

        _registrationApplicationSessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                RegistrationJourney = FrontendSchemeRegistration.Application.Enums.RegistrationJourney.CsoLargeProducer
            });

        var urlMock = new Mock<IUrlHelper>();
        urlMock.Setup(x => x.Content($"~{PagePaths.OrganisationDetailsUploaded}"))
            .Returns($"~{PagePaths.OrganisationDetailsUploaded}");
        _systemUnderTest.Url = urlMock.Object;

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result?.ViewName.Should().Be("FileUploadBrands");
        var backlink = _systemUnderTest.ViewBag.BackLinkToDisplay as string;
        backlink.Should().NotBeNull();
        backlink.Should().Contain($"submissionId={submissionId}");
        backlink.Should().Contain($"registrationyear={registrationYear}");
        backlink.Should().Contain("registrationjourney=CsoLargeProducer");
    }
}