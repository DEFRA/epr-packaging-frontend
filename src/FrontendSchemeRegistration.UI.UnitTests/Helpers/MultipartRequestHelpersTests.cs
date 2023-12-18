namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using FluentAssertions;
using Microsoft.Net.Http.Headers;
using UI.Helpers;

[TestFixture]
public class MultipartRequestHelpersTests
{
    [Test]
    public async Task GetBoundary_ReturnsBoundary_WhenContentTypeIsValid()
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundary7gC7FXxMuHk17Trm";

        // Act
        var result = MultipartRequestHelpers.GetBoundary(contentType, 100);

        // Assert
        result.Should().Be("----WebKitFormBoundary7gC7FXxMuHk17Trm");
    }

    [Test]
    public async Task GetBoundary_ThrowsInvalidDataException_WhenContentTypeDoesNotContainBoundary()
    {
        // Arrange
        const string contentType = "multipart/form-data";

        // Act
        Action action = () => MultipartRequestHelpers.GetBoundary(contentType, 100);

        // Assert
        action.Should().Throw<InvalidDataException>();
    }

    [Test]
    public async Task GetBoundary_ThrowsInvalidDataException_WhenBoundaryLengthIsGreaterThanLengthLimit()
    {
        // Arrange
        const string contentType = "multipart/form-data; boundary=----WebKitFormBoundary7gC7FXxMuHk17Trm";

        // Act
        Action action = () => MultipartRequestHelpers.GetBoundary(contentType, 10);

        // Assert
        action.Should().Throw<InvalidDataException>();
    }

    [Test]
    public async Task IsMultipartContentType_ReturnsFalse_WhenContentTypeIsNull()
    {
        // Arrange
        const string contentType = null;

        // Act
        var result = MultipartRequestHelpers.IsMultipartContentType(contentType);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsMultipartContentType_ReturnsFalse_WhenContentTypeIsEmpty()
    {
        // Arrange
        var contentType = string.Empty;

        // Act
        var result = MultipartRequestHelpers.IsMultipartContentType(contentType);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsMultipartContentType_ReturnsFalse_WhenContentDoesNotContainMultipart()
    {
        // Arrange
        const string contentType = "other";

        // Act
        var result = MultipartRequestHelpers.IsMultipartContentType(contentType);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsMultipartContentType_ReturnsTrue_WhenContentContainsMultipart()
    {
        // Arrange
        const string contentType = "multipart/form-data";

        // Act
        var result = MultipartRequestHelpers.IsMultipartContentType(contentType);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasFileContentDisposition_ReturnsFalse_WhenContentDispositionIsNull()
    {
        // Arrange
        ContentDispositionHeaderValue contentDisposition = null;

        // Act
        var result = MultipartRequestHelpers.HasFileContentDisposition(contentDisposition);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasFileContentDisposition_ReturnsFalse_WhenContentDispositionTypeIsNotFormData()
    {
        // Arrange
        var contentDisposition = new ContentDispositionHeaderValue("other")
        {
            FileName = "filename.csv",
        };

        // Act
        var result = MultipartRequestHelpers.HasFileContentDisposition(contentDisposition);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasFileContentDisposition_ReturnsTrue_WhenContentDispositionFileNameIsNull()
    {
        // Arrange
        var contentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            FileName = null,
        };

        // Act
        var result = MultipartRequestHelpers.HasFileContentDisposition(contentDisposition);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasFileContentDisposition_ReturnsTrue_WhenContentDispositionFileNameIsEmpty()
    {
        // Arrange
        var contentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            FileName = string.Empty,
        };

        // Act
        var result = MultipartRequestHelpers.HasFileContentDisposition(contentDisposition);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasFileContentDisposition_ReturnsTrue_WhenContentDispositionIsValid()
    {
        // Arrange
        var contentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            FileName = "filename.csv",
        };

        // Act
        var result = MultipartRequestHelpers.HasFileContentDisposition(contentDisposition);

        // Assert
        result.Should().BeTrue();
    }
}