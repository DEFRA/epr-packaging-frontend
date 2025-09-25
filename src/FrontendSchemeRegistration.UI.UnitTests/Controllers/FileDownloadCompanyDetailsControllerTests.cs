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
public class FileDownloadCompanyDetailsControllerTests
{
    private static readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid UserId = Guid.NewGuid();
    private readonly Guid OrganisationId = Guid.NewGuid();
    private FileDownloadCompanyDetailsController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IFileDownloadService> _fileDownloadServiceMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _fileDownloadServiceMock = new Mock<IFileDownloadService>();
        _systemUnderTest = new FileDownloadCompanyDetailsController(_submissionServiceMock.Object, _fileDownloadServiceMock.Object);
    }

    [Test]
    public async Task Get_ReturnsBadRequest_WhenMissingSubmissionId()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            Id = _submissionId,
            CompanyDetailsFileName = "testFile01.csv",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid()
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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            Id = _submissionId,
            CompanyDetailsFileName = "testFile01.csv",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "testFile01.csv"
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
        result.FileDownloadName.Should().Be("testFile01.csv");
    }

    [Test]
    public async Task Get_Returns_ValidSubmittedFile()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            Id = _submissionId,
            CompanyDetailsFileName = "testFile01.csv",
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "testFile01.csv"
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
        result.FileDownloadName.Should().Be("testFile01.csv");
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

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { Id = _submissionId, Created = DateTime.Now });

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

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission { Id = _submissionId, Created = DateTime.Now });

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
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission
        {
            Id = _submissionId,
            CompanyDetailsFileName = "testFile01.csv",
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "testFile01.csv"
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
        result.FileDownloadName.Should().Be("testFile01.csv");
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
    public async Task Get_ReturnsNotFound_When_SpecifiedFileDoesNotExistInHistory()
    {
        // Arrange
        var fileIdToSearch = Guid.NewGuid();

        // Setup last submitted files
        var lastSubmittedFiles = new SubmittedRegistrationFilesInformation
        {
            CompanyDetailsFileId = Guid.NewGuid(),
            CompanyDetailsFileName = "submitted.csv"
        };

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                Id = _submissionId,
                Created = DateTime.UtcNow,
                LastSubmittedFiles = lastSubmittedFiles
            });

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
            FileId = fileIdToSearch
        };

        // Act
        var result = await _systemUnderTest.Get(viewModel) as NotFoundResult;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Get_ReturnsFile_When_SpecifiedFileExistsInHistory()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var fileName = "company.csv";

        var submission = new RegistrationSubmission
        {
            Id = submissionId,
            Created = DateTime.UtcNow
        };

        var history = new List<SubmissionHistory>
        {
        new SubmissionHistory { FileId = fileId, FileName = fileName }
        };

        var submissionServiceMock = new Mock<ISubmissionService>();
        var fileDownloadServiceMock = new Mock<IFileDownloadService>();

        submissionServiceMock
            .Setup(s => s.GetSubmissionAsync<RegistrationSubmission>(submissionId))
            .ReturnsAsync(submission);

        submissionServiceMock
            .Setup(s => s.GetSubmissionHistoryAsync(submissionId, It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        fileDownloadServiceMock
            .Setup(f => f.GetFileAsync(fileId, fileName, SubmissionType.Registration, submissionId))
            .ReturnsAsync("csv-data"u8.ToArray());

        var sut = new FileDownloadCompanyDetailsController(submissionServiceMock.Object, fileDownloadServiceMock.Object);

        var model = new FileDownloadViewModel
        {
            SubmissionId = submissionId,
            Type = FileDownloadType.Submission,
            FileId = fileId
        };

        // Act
        var result = await sut.Get(model) as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result.FileDownloadName.Should().Be(fileName);
        result.ContentType.Should().Be("text/csv");
    }

    [Test]
    public async Task Get_ReturnsBadRequest_When_ModelStateInvalidOrSubmissionIdEmpty()
    {
        var model = new FileDownloadViewModel { SubmissionId = Guid.Empty };
        _systemUnderTest.ModelState.AddModelError("dummy", "error");

        var result = await _systemUnderTest.Get(model);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Get_ReturnsBadRequest_WhenSubmissionIdIsEmpty()
    {
        var model = new FileDownloadViewModel { SubmissionId = Guid.Empty };
        _systemUnderTest.ModelState.AddModelError("dummy", "error");

        var result = await _systemUnderTest.Get(model);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Get_ReturnsUploadedFile_WhenTypeIsUpload()
    {
        var fileId = Guid.NewGuid();
        var fileName = "uploaded.csv";

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(_submissionId))
            .ReturnsAsync(new RegistrationSubmission
            {
                Created = DateTime.UtcNow,
                LastUploadedValidFiles = new UploadedRegistrationFilesInformation
                {
                    CompanyDetailsFileId = fileId,
                    CompanyDetailsFileName = fileName
                },
                LastSubmittedFiles = new SubmittedRegistrationFilesInformation
                {
                    CompanyDetailsFileId = Guid.NewGuid(),
                    CompanyDetailsFileName = "irrelevant.csv"
                },
                ValidationPass = true
            });

        _fileDownloadServiceMock.Setup(f => f.GetFileAsync(fileId, fileName, SubmissionType.Registration, _submissionId))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var model = new FileDownloadViewModel { SubmissionId = _submissionId, Type = FileDownloadType.Upload };

        var result = await _systemUnderTest.Get(model) as FileContentResult;

        result.Should().NotBeNull();
        result.FileDownloadName.Should().Be(fileName);
    }

    [Test]
    public async Task Get_ReturnsLastSubmittedFile_WhenFileIdIsNull()
    {
        var fileId = Guid.NewGuid();
        var fileName = "submitted.csv";

        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(_submissionId))
            .ReturnsAsync(new RegistrationSubmission
            {
                Created = DateTime.UtcNow,
                LastUploadedValidFiles = new UploadedRegistrationFilesInformation
                {
                    CompanyDetailsFileId = Guid.NewGuid(),
                    CompanyDetailsFileName = "submitted.csv"
                },
                LastSubmittedFiles = new SubmittedRegistrationFilesInformation
                {
                    CompanyDetailsFileId = fileId,
                    CompanyDetailsFileName = fileName
                },
                ValidationPass = true
            });

        _fileDownloadServiceMock.Setup(f => f.GetFileAsync(fileId, fileName, SubmissionType.Registration, _submissionId))
            .ReturnsAsync(new byte[] { 4, 5, 6 });

        var model = new FileDownloadViewModel { SubmissionId = _submissionId };

        var result = await _systemUnderTest.Get(model) as FileContentResult;

        result.Should().NotBeNull();
        result.FileDownloadName.Should().Be(fileName);
    }

    [Test]
    public async Task Get_ReturnsSpecifiedFile_WhenFileIdProvided()
    {
        // Arrange
        var requestedFileId = Guid.NewGuid();
        var requestedFileName = "specific.csv";

        var submissionHistory = new List<SubmissionHistory>
    {
        new SubmissionHistory { FileId = requestedFileId, FileName = requestedFileName }
    };

        var submission = new RegistrationSubmission
        {
            Created = DateTime.UtcNow,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "specific.csv"
            },
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "specific.csv"
            }
        };

        _submissionServiceMock
            .Setup(s => s.GetSubmissionAsync<RegistrationSubmission>(_submissionId))
            .ReturnsAsync(submission);

        _submissionServiceMock
            .Setup(s => s.GetSubmissionHistoryAsync(_submissionId, It.IsAny<DateTime>()))
            .ReturnsAsync(submissionHistory);

        _fileDownloadServiceMock
            .Setup(f => f.GetFileAsync(requestedFileId, requestedFileName, SubmissionType.Registration, _submissionId))
            .ReturnsAsync(new byte[] { 7, 8, 9 });

        var model = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            FileId = requestedFileId
        };

        // Act
        var result = await _systemUnderTest.Get(model) as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result.FileDownloadName.Should().Be(requestedFileName);
    }

    [Test]
    public async Task Get_ReturnsNotFound_WhenSpecifiedFileDoesNotExist()
    {
        // Arrange
        var requestedFileId = Guid.NewGuid();
        var submission = new RegistrationSubmission
        {
            Created = DateTime.UtcNow,
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileId = Guid.NewGuid(),
                CompanyDetailsFileName = "last.csv"
            }
        };

        // Submission returned by the service
        _submissionServiceMock
            .Setup(s => s.GetSubmissionAsync<RegistrationSubmission>(_submissionId))
            .ReturnsAsync(submission);

        _submissionServiceMock
            .Setup(s => s.GetSubmissionHistoryAsync(_submissionId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SubmissionHistory>
            {
            new SubmissionHistory { FileId = Guid.NewGuid(), FileName = "other.csv" }
            });

        var model = new FileDownloadViewModel
        {
            SubmissionId = _submissionId,
            FileId = requestedFileId,
            Type = FileDownloadType.Submission
        };

        // Act
        var result = await _systemUnderTest.Get(model);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
