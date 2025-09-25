using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class FileDownloadPackagingControllerTests
{
    private static readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid UserId = Guid.NewGuid();
    private readonly Guid OrganisationId = Guid.NewGuid();
    private FileDownloadPackagingController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFileDownloadService> _fileDownloadServiceMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _fileDownloadServiceMock = new Mock<IFileDownloadService>();
        _systemUnderTest = new FileDownloadPackagingController(_submissionServiceMock.Object, _fileDownloadServiceMock.Object);
    }

    [Test]
    public async Task Get_ReturnsBadRequest_WhenMissingSubmissionId()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            Id = _submissionId,
            PomFileName = "testFile01.csv",
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid()
            },
            ValidationPass = true
        });

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        FileDownloadViewModel viewModel = new();

        // Act
        var result = await _systemUnderTest.Get(viewModel);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Get_Returns_ValidUploadedFile()
    {
        // Arrange
        var fileName = "testFile01.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            Id = _submissionId,
            PomFileName = fileName,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = fileName
            },
            ValidationPass = true
        });

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as FileContentResult;

        // Assert
        result.Should().BeOfType<FileContentResult>();
        result.FileDownloadName.Should().Be(fileName);
    }

    [Test]
    public async Task Get_Returns_ValidSubmittedFile()
    {
        // Arrange
        var fileName = "testFile01.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            Id = _submissionId,
            PomFileName = fileName,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = fileName
            },
            ValidationPass = true
        });

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            Type = FileDownloadType.Submission
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as FileContentResult;

        // Assert
        result.Should().BeOfType<FileContentResult>();
        result.FileDownloadName.Should().Be(fileName);
    }

    [Test]
    public async Task Get_Returns_ValidSpecifiedFile()
    {
        // Arrange
        var submissionHistory = new SubmissionHistory
        {
            FileId = Guid.NewGuid(),
            FileName = "testFile01.csv"
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new PomSubmission { Id = _submissionId, Created = DateTime.Now });

        _submissionServiceMock.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SubmissionHistory> { submissionHistory });

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            Type = FileDownloadType.Submission,
            FileId = submissionHistory.FileId
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as FileContentResult;

        // Assert
        result.Should().BeOfType<FileContentResult>();
        result.FileDownloadName.Should().Be(submissionHistory.FileName);
    }

    [Test]
    public async Task Get_ReturnsNotFoundError_WhenSpecifiedFileDoesNotExist()
    {
        // Arrange
        var submissionHistory = new SubmissionHistory
        {
            FileId = Guid.NewGuid(),
            FileName = "testFile01.csv"
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new PomSubmission { Id = _submissionId, Created = DateTime.Now });

        _submissionServiceMock.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SubmissionHistory>());

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            Type = FileDownloadType.Submission,
            FileId = submissionHistory.FileId
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as NotFoundResult;

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Get_ReturnsFileUploadingView_WhenUploadHasNotCompleted()
    {
        // Arrange
        var fileName = "testFile01.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            Id = _submissionId,
            PomFileName = fileName,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = fileName
            },
            ValidationPass = true
        });

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as FileContentResult;

        // Assert
        result.Should().BeOfType<FileContentResult>();
        result.FileDownloadName.Should().Be(fileName);
    }

    private List<Claim> CreateUserDataClaim(string organisationRole, string serviceRole = null)
    {
        var userData = new UserData
        {
            Organisations = new List<Organisation>
                {
                    new()
                    {
                        Id = OrganisationId,
                        OrganisationRole = organisationRole,
                        Name = "Test Name",
                        OrganisationNumber = "Test Number"
                    }
                },
            Id = UserId,
            ServiceRole = serviceRole
        };

        return new List<Claim>
            {
                new (ClaimTypes.UserData, JsonSerializer.Serialize(userData))
            };
    }

    [Test]
    public async Task Get_Should_Return_BadRequest_When_ModelState_Invalid()
    {
        // Arrange
        var model = new FileDownloadViewModel
        {
            SubmissionId = Guid.Empty,  // triggers the condition
            Type = FileDownloadType.Upload
        };

        _systemUnderTest.ModelState.AddModelError("dummy", "error"); // also triggers ModelState invalid

        // Act
        var result = await _systemUnderTest.Get(model);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Get_ReturnsNotFound_When_SpecifiedFileDoesNotExist()
    {
        // Arrange
        var submissionHistory = new List<SubmissionHistory>
        {
        new SubmissionHistory { FileId = Guid.NewGuid(), FileName = "someFile.csv" }
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new PomSubmission { Id = _submissionId, Created = DateTime.Now });

        _submissionServiceMock.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(submissionHistory);

        var mockHttpContext = new Mock<HttpContext>();
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var viewModel = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            FileId = Guid.NewGuid(),
            Type = FileDownloadType.Submission
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as NotFoundResult;

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Get_ReturnsNotFound_When_FileIdNotInHistory()
    {
        // Arrange
        var model = new FileDownloadViewModel
        {
            SubmissionId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            Type = FileDownloadType.Submission
        };

        var submission = new PomSubmission
        {
            Created = DateTime.UtcNow,
            LastSubmittedFile = new SubmittedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = "last.csv"
            }
        };

        _submissionServiceMock
            .Setup(s => s.GetSubmissionAsync<PomSubmission>(model.SubmissionId))
            .ReturnsAsync(submission);

        _submissionServiceMock
            .Setup(s => s.GetSubmissionHistoryAsync(model.SubmissionId, It.IsAny<DateTime>()))
            .ReturnsAsync([]); 

        var controller = new FileDownloadPackagingController(
            _submissionServiceMock.Object,
            _fileDownloadServiceMock.Object);

        // Act
        var result = await controller.Get(model);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Get_ReturnsFileContent_When_FileIdMatchesHistory()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileName = "matchedFile.csv";
        var submissionHistory = new List<SubmissionHistory>
    {
        new SubmissionHistory { FileId = fileId, FileName = fileName }
    };

        _submissionServiceMock.Setup(s => s.GetSubmissionAsync<PomSubmission>(_submissionId))
            .ReturnsAsync(new PomSubmission { Id = _submissionId, Created = DateTime.UtcNow });

        _submissionServiceMock.Setup(s => s.GetSubmissionHistoryAsync(_submissionId, It.IsAny<DateTime>()))
            .ReturnsAsync(submissionHistory);

        _fileDownloadServiceMock.Setup(f => f.GetFileAsync(fileId, fileName, SubmissionType.Producer, _submissionId))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var model = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            FileId = fileId,
            Type = FileDownloadType.Submission
        };

        // Act
        var result = await _systemUnderTest.Get(model) as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result.FileDownloadName.Should().Be(fileName);
        result.FileContents.Should().Equal(new byte[] { 1, 2, 3 });
    }

    [Test]
    public async Task Get_ReturnsNotFound_When_FileIdDoesNotMatchHistory()
    {
        // Arrange
        var existingFileId = Guid.NewGuid();
        var nonMatchingFileId = Guid.NewGuid();

        var submissionHistory = new List<SubmissionHistory>
    {
        new SubmissionHistory { FileId = existingFileId, FileName = "existingFile.csv" }
    };

        _submissionServiceMock.Setup(s => s.GetSubmissionAsync<PomSubmission>(_submissionId))
            .ReturnsAsync(new PomSubmission { Id = _submissionId, Created = DateTime.UtcNow });

        _submissionServiceMock.Setup(s => s.GetSubmissionHistoryAsync(_submissionId, It.IsAny<DateTime>()))
            .ReturnsAsync(submissionHistory);

        var model = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            FileId = nonMatchingFileId,
            Type = FileDownloadType.Submission
        };

        // Act
        var result = await _systemUnderTest.Get(model);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}