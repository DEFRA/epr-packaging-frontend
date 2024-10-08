﻿using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    public class SubsidiariesCompleteFileControllerTests
    {
        private Mock<ISubsidiaryService> _subsidiaryServiceMock;
        private SubsidiariesCompleteFileController _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _subsidiaryServiceMock = new Mock<ISubsidiaryService>();
            _systemUnderTest = new SubsidiariesCompleteFileController(_subsidiaryServiceMock.Object);
        }

        [Test]
        public async Task Get_ReturnsCorrectViewModel_WhenNoErrorsExist()
        {
            // Act
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("SubsidiariesCompleteFile");
        }

        [Test]
        public async Task GetFileUploadTemplate_ReturnsCorrectFile_WhenNoErrorsExist()
        {
            // Arrange
            var expectedFile = new SubsidiaryFileUploadTemplateDto
            {
                ContentType = "text/csv",
                Name = "name.csv",
                Content = new MemoryStream()
            };

            _subsidiaryServiceMock.Setup(x => x.GetFileUploadTemplateAsync()).ReturnsAsync(expectedFile);
            _systemUnderTest.TempData = new Mock<ITempDataDictionary>().Object;

            // Act

            var result = await _systemUnderTest.GetFileUploadTemplate() as FileStreamResult;

            // Assert

            result.FileStream.Should().BeSameAs(expectedFile.Content);
            result.ContentType.Should().Be(expectedFile.ContentType);
            result.FileDownloadName.Should().Be(expectedFile.Name);

            _subsidiaryServiceMock.Verify(x => x.GetFileUploadTemplateAsync(), Times.Once);
        }

        [Test]
        public async Task GetFileUploadTemplate_Redirects_WhenErrorsExist()
        {
            // Arrange
            _subsidiaryServiceMock.Setup(x => x.GetFileUploadTemplateAsync()).ReturnsAsync((SubsidiaryFileUploadTemplateDto)null);

            // Act
            var result = await _systemUnderTest.GetFileUploadTemplate() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("TemplateFileDownloadFailed");

            _subsidiaryServiceMock.Verify(x => x.GetFileUploadTemplateAsync(), Times.Once);
        }

        [Test]
        public void TemplateFileDownload_Redirect_To_Correct_Action()
        {
            // Arrange
            _systemUnderTest.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = _systemUnderTest.TemplateFileDownload() as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull(); 
            result.ActionName.Should().Be("TemplateFileUploadView");  
            result.ControllerName.Should().Be("SubsidiariesCompleteFile");
        }

        [Test]
        public void TemplateFileDownloadFailed_Returns_ViewResult_With_Correct_ViewName()
        {
            // Act
            var result = _systemUnderTest.TemplateFileDownloadFailed() as ViewResult;

            // Assert
            result.ViewName.Should().Be("TemplateFileDownloadFailed");
        }

        [Test]
        public void TemplateFileUploadView_Returns_With_Correct_ViewName()
        {
            // Act
            var result = _systemUnderTest.TemplateFileUploadView() as ViewResult;

            // Assert
            result.ViewName.Should().Be("TemplateFileDownload");
        }

        [Test]
        public async Task GetFileUploadTemplate_ShouldRedirectToTemplateFileDownloadFailed_OnException()
        {
            // Arrange
            _subsidiaryServiceMock.Setup(service => service.GetFileUploadTemplateAsync())
                        .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _systemUnderTest.GetFileUploadTemplate() as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be(nameof(_systemUnderTest.TemplateFileDownloadFailed));
        }
    }
}
