namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Json;
using Application.Enums;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadPartnershipsSuccessControllerTests
{
    private static readonly string SubmissionId = Guid.NewGuid().ToString();

    private FileUploadPartnershipsSuccessController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IRegistrationPeriodProvider> _registrationPeriodProviderMock;
    private Mock<IUrlHelper> _urlHelperMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    Organisations =
                    [
                        new Organisation
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    ]
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
        _registrationPeriodProviderMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        
        _systemUnderTest = new FileUploadPartnershipsSuccessController(_submissionServiceMock.Object, _sessionManagerMock.Object, _registrationPeriodProviderMock.Object);
        _systemUnderTest.Url = _urlHelperMock.Object;
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", SubmissionId },
                    }),
                },
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync((RegistrationSubmission)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadPartnershipsSuccessView_WhenGetSubmissionAsyncReturnsSubmissionDto()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                PartnershipsFileName = fileName,
                RegistrationJourney = RegistrationJourney.CsoSmallProducer
            });

        var userData = new UserData
        {
            Organisations = 
            [
                new Organisation { Name = "Org A" }
            ]
        };
        var claims = new List<Claim> { new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData)) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _systemUnderTest.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadPartnershipsSuccess");
        result.Model.Should().BeEquivalentTo(new FileUploadSuccessViewModel
        {
            FileName = fileName,
            SubmissionId = new Guid(SubmissionId),
            RegistrationYear = DateTime.Now.Year,
            OrganisationName = "Org A",
            RegistrationJourney = RegistrationJourney.CsoSmallProducer
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadPartnerships}?registrationyear={DateTime.Now.Year}&registrationjourney=CsoSmallProducer&submissionId={SubmissionId}");
    }

    [Test]
    public async Task Get_SetsOrganisationName_OnViewModel_FromUserDataClaim()
    {
        // Arrange
        const string fileName = "example.csv";
        const string organisationName = "Test Organisation";

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                PartnershipsFileName = fileName
            });

        var userData = new UserData
        {
            Organisations =
            [
                new Organisation
                {
                    Name = organisationName
                }
            ]
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _systemUnderTest.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewName.Should().Be("FileUploadPartnershipsSuccess");

        var model = result.Model as FileUploadSuccessViewModel;
        model.Should().NotBeNull();
        model!.OrganisationName.Should().Be(organisationName);
    }
}