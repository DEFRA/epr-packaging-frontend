﻿using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
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
}