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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Services.Interfaces;
using UI.Sessions;

[TestFixture]
public class FileUploadControllerTests
{
    private const string SubmissionPeriod = "submissionPeriod";
    private const string ViewName = "FileUpload";
    private const string ContentType = "text/csv";
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFileUploadService> _fileUploadServiceMock;
    private FileUploadController _fileUploadController;

    [SetUp]
    public void SetUp()
    {
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new()
                {
                    SubmissionPeriod = SubmissionPeriod,
                    Journey = new List<string> { PagePaths.FileUploadSubLanding }
                },
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
                }
            });
        _submissionServiceMock = new Mock<ISubmissionService>();
        _fileUploadServiceMock = new Mock<IFileUploadService>();

        _fileUploadController = new FileUploadController(_submissionServiceMock.Object, _fileUploadServiceMock.Object, _sessionManagerMock.Object, Options.Create(new GlobalVariables { FileUploadLimitInBytes = 268435456, SubsidiaryFileUploadLimitInBytes =  61440}));
        _fileUploadController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Query = new QueryCollection() },
                Session = Mock.Of<ISession>()
            },
        };
        _fileUploadController.Url = Mock.Of<IUrlHelper>();
    }

    [Test]
    public async Task Get_ReturnsFileUploadView_WhenCalled()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() }
        });
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission { Id = SubmissionId });

        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task Get_ReturnsFileUploadView_WhenSubmissionIdIsNotInQueryParameters()
    {
        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileUploadView_WhenSubmissionIsNull()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() }
        });
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync((PomSubmission)null);

        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task Get_ReturnsFileUploadView_WhenSubmissionDoesNotContainExceptionCodes()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() }
        });
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission());

        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
    }

    [Test]
    public async Task Get_ReturnsFileUploadViewWithoutErrors_WhenSubmissionContainsExceptionCodesAndShowErrorsQueryParameterIsNotPresent()
    {
        // Arrange
        var exceptionCodes = new List<string> { "99" };
        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() }
        });
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission { Errors = exceptionCodes });

        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.ModelState.ErrorCount.Should().Be(0);
    }

    [Test]
    public async Task Get_ReturnsFileUploadViewWithErrors_WhenSubmissionContainsExceptionCodesAndShowErrorsQueryParameterIsTrue()
    {
        // Arrange
        var exceptionCodes = new List<string> { "99" };

        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() },
            { "showErrors", true.ToString() }
        });
        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission { Errors = exceptionCodes });

        // Act
        var result = await _fileUploadController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Get_ReturnsFileUploadSubLandingView_WhenJourneyDoesNotContainSubLandingPageJourney()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", SubmissionId.ToString() }
        });

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission());

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new()
                {
                    Journey = new List<string> { }
                }
            });

        // Act
        var result = await _fileUploadController.Get() as RedirectToActionResult;

        // Assert
        result?.ControllerName.Should().Be(nameof(FileUploadSubLandingController).RemoveControllerFromName());
        result?.ActionName.Should().Be(nameof(FileUploadSubLandingController.Get));
    }

    [Test]
    public async Task Post_ReturnsFileUploadView_WhenTheModelStateIsInvalid()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.ContentType = ContentType;
        _fileUploadController.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _fileUploadController.Post() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        _fileUploadServiceMock.Verify(
            x => x.ProcessUploadAsync(
                ContentType,
                It.IsAny<Stream>(),
                SubmissionPeriod,
                It.IsAny<ModelStateDictionary>(),
                null,
                SubmissionType.Producer,
                It.IsAny<IFileUploadMessages>(),
                It.IsAny<IFileUploadSize>(),
                null,
                null,
                null),
            Times.Once);
    }

    [Test]
    public async Task Post_RedirectsToFileUploadingWithSubmissionIdQueryParam_WhenTheModelStateIsValid()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.ContentType = ContentType;
        _fileUploadController.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", SubmissionId.ToString() } });

        _fileUploadServiceMock
            .Setup(x =>
                x.ProcessUploadAsync(ContentType, It.IsAny<Stream>(), SubmissionPeriod, new ModelStateDictionary(), SubmissionId, SubmissionType.Producer, It.IsAny<IFileUploadMessages>(), It.IsAny<IFileUploadSize>(), null, null, null))
            .ReturnsAsync(SubmissionId);

        // Act
        var result = await _fileUploadController.Post() as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(FileUploadingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(FileUploadingController.Get));
        result.RouteValues.Should().Contain(new KeyValuePair<string, object?>("submissionId", SubmissionId));
    }

    [Test]
    public async Task Post_AddsFilePathToJourney_WhenTheModelStateIsValid()
    {
        // Arrange
        _fileUploadController.ControllerContext.HttpContext.Request.ContentType = ContentType;
        _fileUploadController.ControllerContext.HttpContext.Request.Query =
            new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", SubmissionId.ToString() } });

        _fileUploadServiceMock
            .Setup(x =>
                x.ProcessUploadAsync(ContentType, It.IsAny<Stream>(), SubmissionPeriod, new ModelStateDictionary(), SubmissionId, SubmissionType.Producer, It.IsAny<IFileUploadMessages>(), It.IsAny<IFileUploadSize>(),

                null, null, null))
            .ReturnsAsync(SubmissionId);

        // Act
        var result = await _fileUploadController.Post() as RedirectToActionResult;

        // Assert
        result?.ControllerName.Should().Be(nameof(FileUploadingController).RemoveControllerFromName());
        result?.ActionName.Should().Be(nameof(FileUploadingController.Get));
        result?.RouteValues.Should().Contain(new KeyValuePair<string, object?>("submissionId", SubmissionId));

        _sessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(),
                It.Is<FrontendSchemeRegistrationSession>(s =>
                    s.RegistrationSession.Journey.Contains(PagePaths.FileUpload))), Times.Once);
    }
}