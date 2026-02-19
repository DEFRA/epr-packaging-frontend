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
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadCompanyDetailsSuccessControllerTests
{
    private static readonly string SubmissionId = Guid.NewGuid().ToString();
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadCompanyDetailsSuccessController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<IRegistrationPeriodProvider> _registrationPeriodProviderMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.UploadingOrganisationDetails);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
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

        _systemUnderTest = new FileUploadCompanyDetailsSuccessController(_submissionServiceMock.Object, _sessionManagerMock.Object, _registrationPeriodProviderMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        {
                            "submissionId", SubmissionId
                        }
                    })
                },
                Session = new Mock<ISession>().Object
            }
        };

        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenGetSubmissionAsyncReturnsNull()
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
                    }
                }
            });

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync((RegistrationSubmission)null);

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
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileUploadSuccessView_WhenGetSubmissionAsyncReturnsSubmissionDto()
    {
        // Arrange
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

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                OrganisationMemberCount = 10
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSuccessViewModel
        {
            FileName = fileName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            IsCso = true
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_SetsIsCsoToFalse_WhenOrganisationRoleIsProducer()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.Producer } } },
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

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                OrganisationMemberCount = 10
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        result.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsSuccessViewModel
        {
            FileName = fileName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            OrganisationRole = OrganisationRoles.Producer,
            IsCso = false
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_IncludesRegistrationJourneyFromQueryParameter_WhenSubmissionHasNoJourney()
    {
        // Arrange
        var registrationJourney = RegistrationJourney.DirectSmallProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.Producer, Name = "Test Org" } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    SubmissionDeadline = DateTime.UtcNow.AddDays(7)
                }
            });

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                RegistrationJourney = null
            });
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(registrationJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        var model = result.Model.As<FileUploadCompanyDetailsSuccessViewModel>();
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
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.Producer, Name = "Test Org" } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    SubmissionDeadline = DateTime.UtcNow.AddDays(7)
                }
            });

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                RegistrationJourney = submissionJourney
            });
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(queryParameterJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        var model = result.Model.As<FileUploadCompanyDetailsSuccessViewModel>();
        model.RegistrationJourney.Should().Be(submissionJourney);
    }

    [Test]
    public async Task Get_IncludesRegistrationJourneyInViewModel_WhenQueryParameterProvided()
    {
        // Arrange
        var registrationJourney = RegistrationJourney.CsoSmallProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.ComplianceScheme, Name = "Test CSO" } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    SubmissionDeadline = DateTime.UtcNow.AddDays(7),
                    IsResubmission = false
                }
            });

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = false,
                RequiresPartnershipsFile = true,
                RegistrationJourney = null
            });
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(registrationJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        var model = result.Model.As<FileUploadCompanyDetailsSuccessViewModel>();
        model.RegistrationJourney.Should().Be(registrationJourney);
        model.IsCso.Should().BeTrue();
        model.FileName.Should().Be(fileName);
    }

    [Test]
    public async Task Get_IncludesSubmissionRegistrationJourneyInModel_WhenBothParameterAndSubmissionHaveValues()
    {
        // Arrange
        var queryParameterJourney = RegistrationJourney.DirectSmallProducer;
        var submissionJourney = RegistrationJourney.CsoLargeProducer;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData { ServiceRole = "Basic User", Organisations = { new Organisation { OrganisationRole = OrganisationRoles.Producer, Name = "Test Org" } } },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    SubmissionDeadline = DateTime.UtcNow.AddDays(7)
                }
            });

        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                CompanyDetailsFileName = fileName,
                CompanyDetailsDataComplete = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                RegistrationJourney = submissionJourney
            });
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        // Act
        var result = await _systemUnderTest.Get(queryParameterJourney) as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsSuccess");
        var model = result.Model.As<FileUploadCompanyDetailsSuccessViewModel>();
        model.RegistrationJourney.Should().Be(submissionJourney);
        model.FileName.Should().Be(fileName);
        model.RequiresBrandsFile.Should().BeTrue();
        model.RequiresPartnershipsFile.Should().BeTrue();
    }
}