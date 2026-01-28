namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadBrandsSuccessControllerTests
{
    private static readonly string SubmissionId = Guid.NewGuid().ToString();
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadBrandsSuccessController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;
    private Mock<ISessionManager<RegistrationApplicationSession>> _registrationApplicationSessionManagerMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();

        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
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
                    }
                }
            });

        _systemUnderTest = new FileUploadBrandsSuccessController(
            _submissionServiceMock.Object,
            _sessionManagerMock.Object,
            _registrationApplicationSessionManagerMock.Object,
            _registrationApplicationServiceMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", SubmissionId }
                    })
                },
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync((RegistrationSubmission?)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadSuccessView_WhenGetSubmissionAsyncReturnsSubmissionDto()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                BrandsFileName = fileName,
                BrandsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                OrganisationMemberCount = 10
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadBrandsSuccess");
        result.Model.Should().BeEquivalentTo(new FileUploadBrandsSuccessViewModel
        {
            FileName = fileName,
            RequiresPartnershipsFile = true,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenRequiresBrandsFileIsFalse()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { RequiresBrandsFile = false });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenBrandsDataCompleteIsFalse()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { BrandsDataComplete = false });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenSessionIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenOrganisationRoleIsNull()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { Organisations = { new Organisation { OrganisationRole = null } } },
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
    public async Task Get_SetsBackLinkToFileUploadBrands_WhenSubmissionIsValid()
    {
        // Arrange
        SetupSessionManagerMockWithComplianceScheme();
        var submissionGuid = Guid.Parse(SubmissionId);
        var registrationYear = DateTime.Now.Year;

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = submissionGuid,
                BrandsFileName = "brands.csv",
                RequiresBrandsFile = true,
                BrandsDataComplete = true,
                RequiresPartnershipsFile = false
            });

        _registrationApplicationSessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                RegistrationJourney = FrontendSchemeRegistration.Application.Enums.RegistrationJourney.CsoSmallProducer
            });

        _registrationApplicationServiceMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), true)).Returns(registrationYear);

        var urlMock = new Mock<IUrlHelper>();
        urlMock.Setup(x => x.Content($"~{PagePaths.FileUploadBrands}"))
            .Returns($"~{PagePaths.FileUploadBrands}");
        _systemUnderTest.Url = urlMock.Object;

        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues>
            {
                { "submissionId", SubmissionId },
                { "registrationyear", registrationYear.ToString() }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadBrandsSuccess");
        var backlink = _systemUnderTest.ViewBag.BackLinkToDisplay as string;
        backlink.Should().NotBeNull();
        backlink.Should().Contain($"submissionId={SubmissionId}");
        backlink.Should().Contain($"registrationyear={registrationYear}");
    }

    private void SetupSessionManagerMockWithComplianceScheme()
    {
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.ComplianceScheme } } },
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
    }
}