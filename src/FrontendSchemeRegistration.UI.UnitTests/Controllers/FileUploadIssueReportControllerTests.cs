using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class FileUploadIssueReportControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private FileUploadIssueReportController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IErrorReportService> _errorReportService;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _errorReportService = new Mock<IErrorReportService>();
        _systemUnderTest = new FileUploadIssueReportController(_submissionServiceMock.Object, _errorReportService.Object);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync((PomSubmission)null);

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _errorReportService.Verify(x => x.GetErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenGetSubmissionDtoDataCompleteIsFalse()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            PomDataComplete = false,
            ValidationPass = false,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _errorReportService.Verify(x => x.GetErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadGet_WhenGetSubmissionDtoPomDataCompleteIsFalse()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(It.IsAny<Guid>())).ReturnsAsync(new PomSubmission
        {
            PomDataComplete = false,
            ValidationPass = true,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUpload");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _errorReportService.Verify(x => x.GetErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileStreamResult_WhenValidationHasFailed()
    {
        // Arrange
        const string fileName = "example.csv";
        const string expectedErrorReportFileName = "example error report.csv";
        var memoryStream = new MemoryStream();

        _errorReportService.Setup(x => x.GetErrorReportStreamAsync(SubmissionId)).ReturnsAsync(memoryStream);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId)).ReturnsAsync(new PomSubmission
        {
            PomDataComplete = true,
            ValidationPass = false,
            PomFileName = fileName,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as FileStreamResult;

        // Assert
        result.FileDownloadName.Should().Be(expectedErrorReportFileName);
        result.FileStream.Should().BeSameAs(memoryStream);
        result.ContentType.Should().Be("text/csv");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _errorReportService.Verify(x => x.GetErrorReportStreamAsync(SubmissionId), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsWarningFileStreamResult_WhenValidationHasPassedWithWarnings()
    {
        // Arrange
        const string fileName = "example.csv";
        const string expectedErrorReportFileName = "example warning report.csv";
        var memoryStream = new MemoryStream();

        _errorReportService.Setup(x => x.GetErrorReportStreamAsync(SubmissionId)).ReturnsAsync(memoryStream);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId)).ReturnsAsync(new PomSubmission
        {
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = true,
            PomFileName = fileName,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as FileStreamResult;

        // Assert
        result.FileDownloadName.Should().Be(expectedErrorReportFileName);
        result.FileStream.Should().BeSameAs(memoryStream);
        result.ContentType.Should().Be("text/csv");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _errorReportService.Verify(x => x.GetErrorReportStreamAsync(SubmissionId), Times.Once);
    }
}