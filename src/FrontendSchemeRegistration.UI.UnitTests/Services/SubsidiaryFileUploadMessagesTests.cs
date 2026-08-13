namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using FluentAssertions;
using FrontendSchemeRegistration.UI.Resources.Views.FileUpload;
using UI.Services.Messages;

[TestFixture]
public class SubsidiaryFileUploadMessagesTests
{
    private readonly SubsidiaryFileUploadMessages _systemUnderTest = new();

    [Test]
    public void SelectACsvFile_ReturnsExpectedResource()
    {
        _systemUnderTest.SelectACsvFile.Should().Be(FileUpload.no_file_uploaded);
    }

    [Test]
    public void TheSelectedFileMustBeACsv_ReturnsExpectedResource()
    {
        _systemUnderTest.TheSelectedFileMustBeACsv.Should().Be(FileUpload.incorrect_file_type);
    }

    [Test]
    public void TheSelectedFileIsEmpty_ReturnsExpectedResource()
    {
        _systemUnderTest.TheSelectedFileIsEmpty.Should().Be(FileUpload.file_did_not_contain_any_data);
    }

    [Test]
    public void TheSelectedFileMustBeSmallerThan_ReturnsExpectedResource()
    {
        _systemUnderTest.TheSelectedFileMustBeSmallerThan.Should().Be(FileUpload.file_size_exceeded_max_allowed);
    }
}
