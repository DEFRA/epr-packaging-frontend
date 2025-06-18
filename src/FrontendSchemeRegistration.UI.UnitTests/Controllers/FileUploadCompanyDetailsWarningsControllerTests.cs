using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class FileUploadCompanyDetailsWarningsControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private FileUploadCompanyDetailsWarningsController _systemUnderTest;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private ValidationOptions _validationOptions;
    private Mock<IUrlHelper> _urlHelperMock;
    private const string SubmissionPeriod = "Jul to Dec 23";
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;


    private List<string> _journey = new()
    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.RegistrationTaskList,
                        PagePaths.ReviewOrganisationData,
                        PagePaths.RegistrationTaskList
    };

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUploadCompanyDetailsSubLanding);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _validationOptions = new ValidationOptions { MaxIssuesToProcess = 100 };
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string> { PagePaths.FileUploadCompanyDetailsSubLanding, PagePaths.FileUploadCompanyDetails, PagePaths.RegistrationTaskList }
                }
            });

        _submissionServiceMock = new Mock<ISubmissionService>();

        _systemUnderTest = new FileUploadCompanyDetailsWarningsController(
            _submissionServiceMock.Object,
            _sessionManagerMock.Object,
            Options.Create(_validationOptions), _registrationApplicationServiceMock.Object);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", SubmissionId.ToString() },
                    }),
                },
                Session = new Mock<ISession>().Object
            },
        };

        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_RedirectsTo_FileUploadCompanyDetailsGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync((PomSubmission)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsGet_WhenGetSubmissionAsyncReturnsSubmissionWithDataCompleteFalse()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            PomDataComplete = false
        });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsWarningsView_WhenGetSubmissionAsyncReturnsCompletedValidSubmissionWithOnlyWarnings()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsFileName = fileName,
            CompanyDetailsDataComplete = true,
            ValidationPass = true,
            HasWarnings = true
        });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsWarnings");
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.FileUploadCompanyDetailsSubLanding}");
        result.Model.Should().BeEquivalentTo(new FileUploadWarningViewModel()
        {
            FileName = fileName,
            SubmissionId = SubmissionId,
            MaxWarningsToProcess = 100
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsWarningsView_When_Session_RegistrationSession_IsFileUploadJourney_Equals_True()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsFileName = fileName,
            CompanyDetailsDataComplete = true,
            ValidationPass = true,
            HasWarnings = true
        });

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionPeriod = SubmissionPeriod,
                    IsFileUploadJourneyInvokedViaRegistration = true,
                    Journey = _journey
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetailsWarnings");
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~/{PagePaths.RegistrationTaskList}");
        result.Model.Should().BeEquivalentTo(new FileUploadWarningViewModel()
        {
            FileName = fileName,
            SubmissionId = SubmissionId,
            MaxWarningsToProcess = 100
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
        _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetails_WhenJourneyDoesNotContain_RequiredPath()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsFileName = fileName,
            CompanyDetailsDataComplete = true,
            ValidationPass = true
        });

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string> { }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result?.ControllerName.Should().Be(nameof(FileUploadCompanyDetailsController).RemoveControllerFromName());
        result?.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
    }

    [Test]
    public async Task Get_ShouldSetBackLink_To_RegistrationTaskList_WhenisFileUploadJourneyInvokedViaRegistrationisFalse()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsFileName = fileName,
            CompanyDetailsDataComplete = true,
            ValidationPass = true,
            HasWarnings = true
        });

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
           .ReturnsAsync(new FrontendSchemeRegistrationSession
           {
               RegistrationSession = new RegistrationSession
               {
                   SubmissionPeriod = SubmissionPeriod,
                   IsFileUploadJourneyInvokedViaRegistration = true,
                   Journey = _journey
               }
           });

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.Content($"~/{PagePaths.RegistrationTaskList}")).Returns($"~/{PagePaths.RegistrationTaskList}");
        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest.Get().Result;
        var webpageBackLink = _systemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        webpageBackLink.Should().Be($"~/{PagePaths.RegistrationTaskList}");
    }

    [Test]
    public async Task Post_ShouldSetBackLink_To_RegistrationTaskList_WhenisFileUploadJourneyInvokedViaRegistrationisFalse()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsFileName = fileName,
            CompanyDetailsDataComplete = true,
            ValidationPass = true,
            HasWarnings = true
        });

        var viewModel = new FileUploadWarningViewModel
        {
            SubmissionId = SubmissionId,
            UploadNewFile = null
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
           .ReturnsAsync(new FrontendSchemeRegistrationSession
           {
               RegistrationSession = new RegistrationSession
               {
                   SubmissionPeriod = SubmissionPeriod,
                   IsFileUploadJourneyInvokedViaRegistration = true,
                   Journey = _journey
               }
           });

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.Content($"~/{PagePaths.RegistrationTaskList}")).Returns($"~/{PagePaths.RegistrationTaskList}");
        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest.FileUploadDecision(viewModel).Result;
        var webpageBackLink = _systemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        webpageBackLink.Should().Be($"~/{PagePaths.RegistrationTaskList}");
    }

    [Test]
    public async Task Post_WhenUserOptsToUploadNew_RedirectsToFileUploadCompanyDetailsGet()
    {
        // Arrange
        var viewModel = new FileUploadWarningViewModel
        {
            SubmissionId = SubmissionId,
            UploadNewFile = true
        };

        // Act
        var result = await _systemUnderTest.FileUploadDecision(viewModel) as RedirectToActionResult;

        // Assert
        result?.ControllerName.Should().Be(nameof(FileUploadCompanyDetailsController).RemoveControllerFromName());
        result?.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
    }

    [Test]
    public async Task Post_WhenUserOptsNotToUploadNew_RedirectsToReviewCompanyDetailsGet()
    {
        // Arrange
        var viewModel = new FileUploadWarningViewModel
        {
            SubmissionId = SubmissionId,
            UploadNewFile = false
        };

        // Act
        var result = await _systemUnderTest.FileUploadDecision(viewModel) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result?.ControllerName.Should().Be(nameof(ReviewCompanyDetailsController).RemoveControllerFromName());
        result?.ActionName.Should().Be(nameof(ReviewCompanyDetailsController.Get));
        result.RouteValues["submissionId"].Should().Be(SubmissionId);
    }
}
