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
using FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using TestHelpers;
using UI.Controllers;
using UI.Services.Interfaces;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadCompanyDetailsControllerTests
{
    private const string ContentType = "text/csv";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private static readonly Guid _registrationSetId = Guid.NewGuid();
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFileUploadService> _fileUploadServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadCompanyDetailsController _systemUnderTest;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;

    [SetUp]
    public void SetUp()
    {
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
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    }
                }
            });

        _fileUploadServiceMock = new Mock<IFileUploadService>();
        _registrationApplicationServiceMock.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        _systemUnderTest = new FileUploadCompanyDetailsController
            (_submissionServiceMock.Object,
            _fileUploadServiceMock.Object,
            _sessionManagerMock.Object,
            Options.Create(new GlobalVariables { FileUploadLimitInBytes = 268435456, SubsidiaryFileUploadLimitInBytes = 61440 }), _registrationApplicationServiceMock.Object);
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
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _systemUnderTest.Url = urlHelperMock.Object;
    }

    [Test]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.NewGuid))]
    [TestCase(null)]
    public async Task Get_ReturnsFileUploadCompanyDetailsView_WhenCalled(Guid? registrationSetId)
    {
        // Arrange
        var submissionDeadline = DateTime.UtcNow.Date;
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = ContentType
                },
                Session = new Mock<ISession>().Object
            }
        };

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
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    LatestRegistrationSet = new Dictionary<string, Guid>
                    {
                        {
                            SubmissionPeriod, registrationSetId ?? _registrationSetId
                        }
                    }
                },
                UserData = new UserData
                {
                    Organisations =
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.ComplianceScheme
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result?.ViewName.Should().Be("FileUploadCompanyDetails");
        result?.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsViewModel
        {
            SubmissionDeadline = submissionDeadline,
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            RegistrationYear = DateTime.Now.Year
        });
    }

    [Test]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.NewGuid))]
    [TestCase(null)]
    public async Task Get_ReturnsFileUploadCompanyDetailsView_WhenCalledAndSubmissionIdIsNull(Guid? registrationSetId)
    {
        // Arrange
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = ContentType
                },
                Session = new Mock<ISession>().Object
            }
        };
        var submissionDeadline = DateTime.UtcNow.Date;
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = Guid.NewGuid(),
            });
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SubmissionDeadline = submissionDeadline,
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    },
                    LatestRegistrationSet = new Dictionary<string, Guid>
                    {
                        {
                            SubmissionPeriod, registrationSetId ?? _registrationSetId
                        }
                    }
                },
                UserData = new UserData
                {
                    Organisations =
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.ComplianceScheme
                        }
                    }
                }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result?.ViewName.Should().Be("FileUploadCompanyDetails");
        result?.Model.Should().BeEquivalentTo(new FileUploadCompanyDetailsViewModel
        {
            SubmissionDeadline = submissionDeadline,
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            RegistrationYear = DateTime.Now.Year
        });
    }

    [Test]
    public async Task Get_ReturnsFileUploadCompanyDetailsSubLandingView_WhenSessionDoesnotContainFileUploadCompanyDetailsSubLandingJourney()
    {
        // Arrange
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
                     Journey = new List<string>
                    {
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
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_AddModelStateError_WhenSubmissionContainsErrors()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = Guid.NewGuid(),
                Errors = new List<string> { "Invalid Submission Id" }
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadCompanyDetails");
        result.ViewData.ModelState["file"].Errors[0].ErrorMessage.Should().Be("Invalid Submission Id");
    }

    [Test]
    public async Task Get_ShouldSetBackLink_To_RegistrationTaskList_WhenisFileUploadJourneyInvokedViaRegistrationisFalse()
    {
        // Arrange
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
                   IsFileUploadJourneyInvokedViaRegistration = true,
                   Journey = new List<string>
                   {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                   }
               }
           });

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.Content($"~/{PagePaths.RegistrationTaskList}")).Returns($"~/{PagePaths.RegistrationTaskList}");
        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest.Get().Result;
        var model = (result as ViewResult).Model as FileUploadCompanyDetailsViewModel;
        var webpageBackLink = _systemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        webpageBackLink.Should().Be($"~/{PagePaths.RegistrationTaskList}?registrationyear={DateTime.Now.Year}");
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

    [Test]
    public async Task Post_ReturnsFileUploadCompanyDetailsView_WhenTheModelStateIsInvalid()
    {
        // Arrange
        const string contentType = "content-type";

        _systemUnderTest.ControllerContext.HttpContext.Request.ContentType = contentType;
        _systemUnderTest.ControllerContext.HttpContext.Request.Query = new QueryCollection();
        _systemUnderTest.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _systemUnderTest.Post(It.IsAny<string>()) as ViewResult;

        // Assert
        _fileUploadServiceMock.Verify(
            x => x.ProcessUploadAsync(
                contentType,
                It.IsAny<Stream>(),
                SubmissionPeriod,
                It.IsAny<ModelStateDictionary>(),
                null,
                SubmissionType.Registration,
                It.IsAny<IFileUploadMessages>(),
                It.IsAny<IFileUploadSize>(),
                SubmissionSubType.CompanyDetails,
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>()),
            Times.Once);
        result.ViewName.Should().Be("FileUploadCompanyDetails");
    }

    [Test]
    public async Task Post_RedirectsTo_UploadingOrganisationDetails_WhenTheModelStateIsValid()
    {
        // Arrange
        const string contentType = "content-type";
        var submissionId = Guid.NewGuid();

        _systemUnderTest.ControllerContext.HttpContext.Request.ContentType = contentType;
        _systemUnderTest.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues>
            {
                {
                    "submissionId", submissionId.ToString()
                }
            });

        _fileUploadServiceMock
            .Setup(x => x.ProcessUploadAsync(
                contentType,
                It.IsAny<Stream>(),
                SubmissionPeriod,
                It.IsAny<ModelStateDictionary>(),
                submissionId,
                SubmissionType.Registration,
                It.IsAny<IFileUploadMessages>(),
                It.IsAny<IFileUploadSize>(),
                SubmissionSubType.CompanyDetails,
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>()))
            .ReturnsAsync(submissionId);

        // Act
        var result = await _systemUnderTest.Post(It.IsAny<string>()) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Contain("UploadingOrganisationDetails");
        result.RouteValues.Should().ContainKey("submissionId").WhoseValue.Should().Be(submissionId);
    }
}