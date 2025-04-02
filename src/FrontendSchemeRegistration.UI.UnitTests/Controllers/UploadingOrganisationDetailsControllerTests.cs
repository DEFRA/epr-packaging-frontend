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
using UI.Controllers.ControllerExtensions;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class UploadingOrganisationDetailsControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private UploadingOrganisationDetailsController _systemUnderTest;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;

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
                    }
                }
            });

        _systemUnderTest = new UploadingOrganisationDetailsController(_submissionServiceMock.Object, _sessionManagerMock.Object);
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
                    })
                },
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsSuccessGet_WhenUploadHasCompletedAndContainsNoErrors()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsDataComplete = true
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSuccess");
        result.RouteValues.Should().HaveCount(1).And.ContainKey("submissionId").WhoseValue.Should().Be(SubmissionId.ToString());
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsGet_WhenUploadHasCompletedAndContainsErrors()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsDataComplete = true,
            Errors = new List<string> { "89" }
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");
        result.RouteValues.Should().HaveCount(1).And.ContainKey("submissionId").WhoseValue.Should().Be(SubmissionId.ToString());
    }

    [Test]
    public async Task Get_ReturnsFileUploadingViewModel_WhenCompanyDetailDataHasNotFinishedProcessing()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsDataComplete = false,
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("UploadingOrganisationDetails");
        result.Model.As<FileUploadingViewModel>().SubmissionId.Should().Be(SubmissionId.ToString());
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetails_WhenNoSubmissionIsFound()
    {
        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadCompanyDetailsController).RemoveControllerFromName());
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsErrorsGet_WhenUploadHasCompletedAndContainsRowErrors()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsDataComplete = true,
            RowErrorCount = 150
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsErrors");
        result.RouteValues.Should().HaveCount(1).And.ContainKey("submissionId").WhoseValue.Should().Be(SubmissionId.ToString());
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsWarningsGet_WhenUploadHasCompletedAndContains_RowWarnings()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            CompanyDetailsDataComplete = true,
            RowErrorCount = 0,
            HasWarnings = true
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsWarnings");
        result.RouteValues.Should().HaveCount(1).And.ContainKey("submissionId").WhoseValue.Should().Be(SubmissionId.ToString());
    }

    [Test]
    public async Task Get_RedirectsToUploadingOrganisationDetails_WhenSessionIsNull()
    {
        // Arrange
        var submission = new RegistrationSubmission
        {
            Id = SubmissionId,
            BrandsDataComplete = false
        };

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(submission);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("UploadingOrganisationDetails");
        result.Model.As<FileUploadingViewModel>().SubmissionId.Should().Be(SubmissionId.ToString());
    }
}