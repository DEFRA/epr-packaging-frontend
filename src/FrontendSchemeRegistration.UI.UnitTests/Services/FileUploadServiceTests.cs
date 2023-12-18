namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using System.Globalization;
using System.Text;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;
using TestHelpers;
using UI.Services;
using UI.Services.Interfaces;

[TestFixture]
public class FileUploadServiceTests
{
    private const string SubmissionPeriod = "Jul to Dec 23";
    private ModelStateDictionary _modelStateDictionary;
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private IFileUploadService _fileUploadService;

    [SetUp]
    public void Setup()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
        _modelStateDictionary = new ModelStateDictionary();
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _fileUploadService = new FileUploadService(
            _webApiGatewayClientMock.Object,
            Options.Create(new GlobalVariables { FileUploadLimitInBytes = 1024 }));
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.GenerateRegistrationInformation))]
    public async Task ProcessUpload_AddsErrorToModelState_WhenUploadContentTypeIsNotMultipart(
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        // Arrange
        const string contentType = "other";
        using var fileStream = new MemoryStream();

        // Act
        await _fileUploadService.ProcessUploadAsync(
            contentType,
            fileStream,
            SubmissionPeriod,
            _modelStateDictionary,
            null,
            submissionType,
            submissionSubType,
            registrationSetId,
            complianceSchemeId);

        // Assert
        GetModelStateErrors().Should().HaveCount(1).And.Contain("Select a CSV file");
        _webApiGatewayClientMock.Verify(
            x => x.UploadFileAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                SubmissionPeriod,
                null,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId),
            Times.Never);
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.GenerateRegistrationInformation))]
    public async Task ProcessUpload_AddsErrorToModelState_WhenContentDispositionHeaderIsMissing(
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryaTDX1qGUHmeOnwjh";
        const string streamContent =
            "------WebKitFormBoundaryaTDX1qGUHmeOnwjh\r\nContent-Type: text/csv\r\n\r\ncolumnOne,columnTwo\r\n------WebKitFormBoundaryaTDX1qGUHmeOnwjh--\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamContent));

        // Act
        await _fileUploadService.ProcessUploadAsync(
            contentType,
            stream,
            SubmissionPeriod,
            _modelStateDictionary,
            null,
            submissionType,
            submissionSubType,
            registrationSetId,
            complianceSchemeId);

        // Assert
        GetModelStateErrors().Should().HaveCount(1).And.Contain("File upload is invalid - try again");
        _webApiGatewayClientMock.Verify(
            x => x.UploadFileAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                SubmissionPeriod,
                null,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId),
            Times.Never);
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.CompanyDetails)]
    public async Task ProcessUpload_AddsErrorToModelState_WhenContentDispositionTypeIsNotFormData(
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryaTDX1qGUHmeOnwjh";
        const string streamContent =
            "------WebKitFormBoundaryaTDX1qGUHmeOnwjh\r\nContent-Disposition: invalid; name=\"file\"; filename=\"temp.csv\"\r\nContent-Type: text/csv\r\n\r\ncolumnOne,columnTwo\r\n------WebKitFormBoundaryaTDX1qGUHmeOnwjh--\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamContent));

        // Act
        await _fileUploadService.ProcessUploadAsync(
            contentType,
            stream,
            SubmissionPeriod,
            _modelStateDictionary,
            null,
            submissionType,
            submissionSubType,
            registrationSetId,
            complianceSchemeId);

        // Assert
        GetModelStateErrors().Should().HaveCount(1).And.Contain("Select a CSV file");
        _webApiGatewayClientMock.Verify(
            x => x.UploadFileAsync(
                It.IsAny<byte[]>(),
                "temp.csv",
                SubmissionPeriod,
                null,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId),
            Times.Never);
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.CompanyDetails)]
    public async Task ProcessUpload_DoesNotCallWebApiGatewayClient_WhenFormHelperAddsErrorToModelState(
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryaTDX1qGUHmeOnwjh";
        const string streamContent =
            "------WebKitFormBoundaryaTDX1qGUHmeOnwjh\r\nContent-Disposition: form-data; name=\"file\"; filename=\"temp.pdf\"\r\nContent-Type: text/csv\r\n\r\ncolumnOne,columnTwo\r\n------WebKitFormBoundaryaTDX1qGUHmeOnwjh--\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamContent));

        // Act
        await _fileUploadService.ProcessUploadAsync(
            contentType,
            stream,
            SubmissionPeriod,
            _modelStateDictionary,
            null,
            submissionType,
            submissionSubType,
            registrationSetId,
            complianceSchemeId);

        // Assert
        GetModelStateErrors().Should().HaveCount(1).And.Contain("The selected file must be a CSV");
        _webApiGatewayClientMock.Verify(
            x => x.UploadFileAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                SubmissionPeriod,
                null,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId),
            Times.Never);
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCase(SubmissionType.Producer, null, null, null, true)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.CompanyDetails)]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.GenerateRegistrationInformationWithSubId), new object[] { false })]
    [TestCaseSource(typeof(TestCaseHelper), nameof(TestCaseHelper.GenerateRegistrationInformationWithSubId), new object[] { true })]
    public async Task ProcessUpload_CallsWebApiGatewayClient_WhenFileIsValid(
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null,
        bool withSubmissionId = false)
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryaTDX1qGUHmeOnwjh";
        const string streamContent =
            "------WebKitFormBoundaryaTDX1qGUHmeOnwjh\r\nContent-Disposition: form-data; name=\"file\"; filename=\"temp.csv\"\r\nContent-Type: text/csv\r\n\r\ncolumnOne,columnTwo\r\n------WebKitFormBoundaryaTDX1qGUHmeOnwjh--\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamContent));
        Guid? submissionId = withSubmissionId ? Guid.NewGuid() : null;

        // Act
        await _fileUploadService.ProcessUploadAsync(contentType, stream, SubmissionPeriod, _modelStateDictionary, submissionId, submissionType, submissionSubType, registrationSetId, complianceSchemeId);

        // Assert
        GetModelStateErrors().Should().BeEmpty();
        _webApiGatewayClientMock.Verify(
            x => x.UploadFileAsync(
                It.IsAny<byte[]>(),
                "temp.csv",
                SubmissionPeriod,
                submissionId,
                submissionType,
                submissionSubType,
                registrationSetId,
                complianceSchemeId),
            Times.Once);
    }

    private IEnumerable<string> GetModelStateErrors()
    {
        return _modelStateDictionary.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
    }
}