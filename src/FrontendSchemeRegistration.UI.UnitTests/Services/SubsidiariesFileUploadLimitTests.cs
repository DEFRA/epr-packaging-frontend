namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using Application.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UI.Services.FileUploadLimits;

[TestFixture]
public class SubsidiariesFileUploadLimitTests
{
    [Test]
    public void FileUploadLimitInBytes_ReturnsValueFromGlobalVariables()
    {
        var options = new Mock<IOptions<GlobalVariables>>();
        options.Setup(x => x.Value).Returns(new GlobalVariables { SubsidiaryFileUploadLimitInBytes = 12345 });

        var systemUnderTest = new SubsidiariesFileUploadLimit(options.Object);

        systemUnderTest.FileUploadLimitInBytes.Should().Be(12345);
    }
}
