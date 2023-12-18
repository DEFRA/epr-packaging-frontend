namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using System.Globalization;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using UI.Helpers;

[TestFixture]
public class FileHelpersTests
{
    private const string UploadFileName = "file";
    private ModelStateDictionary _modelStateDictionary;

    [SetUp]
    public void SetUp()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
        _modelStateDictionary = new ModelStateDictionary();
    }

    [Test]
    public async Task ProcessFile_AddsErrorToModelState_WhenTheFileIsEmpty()
    {
        // Arrange
        const int maxUploadSizeInBytes = 100;
        const string fileName = "filename.csv";
        var multipartSection = new MultipartSection
        {
            Body = new MemoryStream(Array.Empty<byte>()),
        };

        // Act
        var result = await FileHelpers.ProcessFileAsync(multipartSection, fileName, _modelStateDictionary, UploadFileName, maxUploadSizeInBytes);

        // Assert
        result.Should().BeOfType<byte[]>().And.BeEmpty();
        GetModelStateErrors().Should().HaveCount(1).And.Contain("The selected file is empty");
    }

    [Test]
    public async Task ProcessFile_AddsErrorToModelState_WhenFileIsLargerThanMaxUploadSize()
    {
        // Arrange
        const int maxUploadSizeInBytes = 1048576;
        const string fileName = "filename.csv";
        var multipartSection = new MultipartSection
        {
            Body = new MemoryStream(new byte[1048577]),
        };

        // Act
        var result = await FileHelpers.ProcessFileAsync(multipartSection, fileName, _modelStateDictionary, UploadFileName, maxUploadSizeInBytes);

        // Assert
        result.Should().BeOfType<byte[]>().And.BeEmpty();
        GetModelStateErrors().Should().HaveCount(1).And.Contain("The selected file must be smaller than 1MB");
    }

    [Test]
    public async Task ProcessFile_AddsErrorToModelState_WhenFileExtensionIsNotCsv()
    {
        // Arrange
        const int maxUploadSizeInBytes = 100;
        const string fileName = "filename.pdf";
        var multipartSection = new MultipartSection
        {
            Body = new MemoryStream(new byte[3]),
        };

        // Act
        var result = await FileHelpers.ProcessFileAsync(multipartSection, fileName, _modelStateDictionary, UploadFileName, maxUploadSizeInBytes);

        // Assert
        result.Should().BeOfType<byte[]>().And.BeEmpty();
        GetModelStateErrors().Should().HaveCount(1).And.Contain("The selected file must be a CSV");
    }

    [Test]
    [TestCase("")]
    [TestCase(null)]
    public async Task ProcessFile_AddsErrorToModelState_WhenNoFileSelected(string fileName)
    {
        // Arrange
        const int maxUploadSizeInBytes = 100;
        var multipartSection = new MultipartSection
        {
            Body = new MemoryStream(new byte[3])
        };

        // Act
        var result = await FileHelpers.ProcessFileAsync(multipartSection, fileName, _modelStateDictionary, UploadFileName, maxUploadSizeInBytes);

        // Assert
        result.Should().BeOfType<byte[]>().And.BeEmpty();
        GetModelStateErrors().Should().HaveCount(1).And.Contain("Select a CSV file");
    }

    [Test]
    public async Task ProcessFile_ReturnsByteArray_WhenFileIsValid()
    {
        // Arrange
        const int maxUploadSizeInBytes = 100;
        const string fileName = "filename.csv";
        var multipartSection = new MultipartSection
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes("file-content")),
        };

        // Act
        var result = await FileHelpers.ProcessFileAsync(multipartSection, fileName, _modelStateDictionary, UploadFileName, maxUploadSizeInBytes);

        // Assert
        result.Should().BeOfType<byte[]>().And.NotBeEmpty();
        GetModelStateErrors().Should().BeEmpty();
    }

    private IEnumerable<string> GetModelStateErrors()
    {
        return _modelStateDictionary.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
    }
}
