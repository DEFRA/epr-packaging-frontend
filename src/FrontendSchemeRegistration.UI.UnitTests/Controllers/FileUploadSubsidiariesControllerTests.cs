using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
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
        private FileUploadSubsidiariesController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockSubmissionService = new Mock<ISubmissionService>();
            _controller = new FileUploadSubsidiariesController(_mockFileUploadService.Object, _mockSubmissionService.Object);
            // Mock HttpContext
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();

            // Setup Request properties
            mockRequest.Setup(r => r.ContentType).Returns("multipart/form-data");
            mockRequest.Setup(r => r.Body).Returns(Stream.Null);

            // Set the HttpContext's Request to our mockRequest
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            // Assign HttpContext to ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
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
            result.As<ViewResult>().Model.Should().BeOfType<FileUploadViewModel>();
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
    }
}
