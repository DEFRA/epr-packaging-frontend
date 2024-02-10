using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class FileUploadCompanyDetailsIssueReportControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private FileUploadCompanyDetailsIssueReportController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IErrorReportService> _registrationErrorReportService;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _registrationErrorReportService = new Mock<IErrorReportService>();
        _systemUnderTest = new FileUploadCompanyDetailsIssueReportController(_submissionServiceMock.Object, _registrationErrorReportService.Object);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync((RegistrationSubmission)null);

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId), Times.Once);
        _registrationErrorReportService.Verify(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsSubLandingGet_WhenValidationPassIsTrue()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission()
        {
            CompanyDetailsDataComplete = true,
            ValidationPass = true,
            HasValidFile = false,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId), Times.Once);
        _registrationErrorReportService.Verify(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_RedirectsFileUploadCompanyDetailsSubLandingGet_WhenCompanyDetailsDataCompleteIsFalse()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync(new RegistrationSubmission()
        {
            CompanyDetailsDataComplete = false,
            ValidationPass = true,
            HasValidFile = true
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId), Times.Once);
        _registrationErrorReportService.Verify(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsFileStreamResult_WhenValidationHasFailed()
    {
        // Arrange
        const string fileName = "example.csv";
        const string expectedErrorReportFileName = "example error report.csv";
        var memoryStream = new MemoryStream();

        _registrationErrorReportService.Setup(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId)).ReturnsAsync(memoryStream);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsDataComplete = true,
            ValidationPass = false,
            CompanyDetailsFileName = fileName,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as FileStreamResult;

        // Assert
        result.FileDownloadName.Should().Be(expectedErrorReportFileName);
        result.FileStream.Should().BeSameAs(memoryStream);
        result.ContentType.Should().Be("text/csv");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId), Times.Once);
        _registrationErrorReportService.Verify(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId), Times.Once);
    }

    [Test]
    public async Task Get_RedirectsFileUploadCompanyDetailsSubLandingGet_WhenStreamIsEmpty()
    {
        // Arrange
        _registrationErrorReportService.Setup(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId)).ReturnsAsync((MemoryStream)null);
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId)).ReturnsAsync(new RegistrationSubmission
        {
            CompanyDetailsDataComplete = true,
            ValidationPass = false,
        });

        // Act
        var result = await _systemUnderTest.Get(SubmissionId) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetailsSubLanding");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(SubmissionId), Times.Once);
        _registrationErrorReportService.Verify(x => x.GetRegistrationErrorReportStreamAsync(SubmissionId), Times.Once);
    }
}