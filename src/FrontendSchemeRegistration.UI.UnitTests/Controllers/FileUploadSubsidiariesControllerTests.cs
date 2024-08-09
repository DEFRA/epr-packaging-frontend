using System.Security.Claims;
using System.Text.Json;
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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    [TestFixture]
    public class FileUploadSubsidiariesControllerTests
    {
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ISubmissionService> _mockSubmissionService;
        private Mock<ISubsidiaryService> _mockSubsidiaryService;
        private FileUploadSubsidiariesController _controller;
        private Mock<ClaimsPrincipal> _claimsPrincipalMock;

        [SetUp]
        public void SetUp()
        {
            _mockFileUploadService = new Mock<IFileUploadService>();
            _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            _mockSubmissionService = new Mock<ISubmissionService>();
            _mockSubsidiaryService = new Mock<ISubsidiaryService>();
            _controller = new FileUploadSubsidiariesController(_mockFileUploadService.Object, _mockSubmissionService.Object, _mockSubsidiaryService.Object);
            var mockRequest = new Mock<HttpRequest>();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object
                }
            };
        }

        [Test]
        public void Index_ShouldReturnViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().BeNull();
        }

        [Test]
        public async Task Post_WhenModelStateIsValid_ShouldRedirectToFileUploading()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            _mockFileUploadService
            .Setup(service => service.ProcessUploadAsync(
                It.IsAny<string?>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ModelStateDictionary>(),
                It.IsAny<Guid?>(),
                It.IsAny<SubmissionType>(),
                It.IsAny<SubmissionSubType?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(submissionId);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.ContentType).Returns("multipart/form-data");
            mockHttpContext.Setup(c => c.Request.Body).Returns(Stream.Null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.Post();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("FileUploading");
            result.As<RedirectToActionResult>().RouteValues["submissionId"].Should().Be(submissionId);
        }

        [Test]
        public async Task Post_WhenModelStateIsInvalid_ShouldReturnViewResult()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Test error");

            // Act
            var result = await _controller.Post();

            // Assert
            result.Should().BeOfType<ViewResult>()
                  .Which.ViewName.Should().Be("Index");
            result.As<ViewResult>().Model.Should().BeOfType<FileUploadSubsidiaryViewModel>();
        }

        [Test]
        public async Task FileUploading_WhenSubmissionIsNull_ShouldRedirectToFileUploadGet()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            _mockSubmissionService
                .Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>()))
                .ReturnsAsync((SubsidiarySubmission)null);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploading();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("Get");
            result.As<RedirectToActionResult>().ControllerName.Should().Be("FileUpload");
        }

        [Test]
        public async Task FileUploading_WhenSubmissionIsComplete_ShouldRedirectToFileUploadSuccess()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var submission = new SubsidiarySubmission
            {
                SubsidiaryDataComplete = true,
                RecordsAdded = 5,
                Errors = new List<string>()
            };
            _mockSubmissionService
                .Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>()))
                .ReturnsAsync(submission);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploading();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                  .Which.ActionName.Should().Be("FileUplodSuccess");
            result.As<RedirectToActionResult>().RouteValues["recordsAdded"].Should().Be(submission.RecordsAdded);
        }

        [Test]
        public async Task FileUplodSuccess_ShouldReturnViewResultWithModel()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["recordsAdded"]).Returns("5");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUplodSuccess();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("FileUplodSuccess");
            var model = viewResult.Model.Should().BeOfType<SubsidiaryFileUplodSuccessViewModel>().Subject;
            model.RecordsAdded.Should().Be(5);
        }

        [Test]
        public async Task ExportSubsidiaries_ReturnsFileResultWithCorrectContentTypeAndFileName()
        {
            // Arrange
            var subsidiaryParentId = 123;
            var mockStream = new MemoryStream();

            var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(subsidiaryParentId, true))
                .ReturnsAsync(mockStream);

            // Act
            var result = await _controller.ExportSubsidiaries(subsidiaryParentId);

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().Be("subsidiary.csv");
            fileResult.FileStream.Should().BeSameAs(mockStream);

            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(subsidiaryParentId, true), Times.Once);
        }

        [Test]
        public async Task ExportSubsidiaries_CallsGetSubsidiariesStreamAsync()
        {
            // Arrange
            var subsidiaryParentId = 123;
            var mockStream = new MemoryStream();
            var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(subsidiaryParentId, true))
                .ReturnsAsync(mockStream);

            // Act
            var result = await _controller.ExportSubsidiaries(subsidiaryParentId);

            // Assert
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(subsidiaryParentId, true), Times.Once);
        }

        private static List<Claim> CreateUserDataClaim(string organisationRole)
        {
            var userData = new UserData
            {
                Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganisationRole = organisationRole
                }
            }
            };

            return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
        }
    }
}
