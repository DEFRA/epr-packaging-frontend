namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using Application.Enums;
using FluentAssertions;
using UI.Extensions;

[TestFixture]
public class EnumExtensionsTests
{
    [Test]
    public void GetLocalizedName_ReturnsCorrectName()
    {
        // Arrange // Act
        var result = SubmissionPeriodStatus.NotStarted.GetLocalizedName();

        // Assert
        result.Should().Be("not_started");
    }
}