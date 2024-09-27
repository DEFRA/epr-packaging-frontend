namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services.Messages;
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

        _systemUnderTest = new FileUploadBrandsController(
            _submissionServiceMock.Object, 
            _fileUploadServiceMock.Object, 
            _sessionManagerMock.Object,
            Options.Create(new GlobalVariables { FileUploadLimitInBytes = 268435456, SubsidiaryFileUploadLimitInBytes = 61440 }));
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
                _submissionPeriod,
                It.IsAny<ModelStateDictionary>(),
                submissionId,
                SubmissionType.Registration,
                It.IsAny<IFileUploadMessages>(),
                It.IsAny<IFileUploadSize>(),
                SubmissionSubType.Brands,
                _registrationSetId,
                null))
            .ReturnsAsync(submissionId);

        // Act
        var result = await _systemUnderTest.Post() as RedirectToActionResult;

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
                _submissionPeriod,
                It.IsAny<ModelStateDictionary>(),
                submissionId,
                SubmissionType.Registration,
                new DefaultFileUploadMessages(),
                It.IsAny<IFileUploadSize>(),
                SubmissionSubType.Brands,
                _registrationSetId,
                null))
            .ReturnsAsync(submissionId);
        _systemUnderTest.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _systemUnderTest.Post() as ViewResult;

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
}