namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using System.Globalization;
using System.Text;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services.FileUploadLimits;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Messages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;
using TestHelpers;
using UI.Services;

[TestFixture]
public class FileDownloadServiceTests
{
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private FileDownloadService _fileDownloadService;

    [SetUp]
    public void Setup()
    {
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _fileDownloadService = new FileDownloadService(_webApiGatewayClientMock.Object);
    }

    [Test]
    public async Task GetFileAsync_ReturnsByteArray_WhenDownloadSuccessful()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileName = "testfile001.csv";
        var submissionType = SubmissionType.Registration;
        var submissionId = Guid.NewGuid();
        Random rnd = new Random();
        byte[] data = new byte[10];
        rnd.NextBytes(data);

        _webApiGatewayClientMock.Setup(service => service.FileDownloadAsync(It.IsAny<string>())).ReturnsAsync(data);

        // Act
        var result = await _fileDownloadService.GetFileAsync(fileId, fileName, submissionType, submissionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(data);
    }
}