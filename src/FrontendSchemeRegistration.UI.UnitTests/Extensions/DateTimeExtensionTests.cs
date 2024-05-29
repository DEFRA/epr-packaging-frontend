namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using UI.Extensions;

[TestFixture]
public class DateTimeExtensionTests
{
    [Test]
    public void ToReadableDate_ReturnsCorrectDateString_WhenGivenADateTime()
    {
        // Arrange
        var datetime = DateTime.Parse("2023/05/04 5:00:00 AM");

        // Act
        var result = datetime.ToReadableDate();

        // Assert
        result.Should().Be("4 May 2023");
    }

    [Test]
    public void ToShortReadableDate_ReturnsCorrectDateString_WhenGivenADateTime()
    {
        // Arrange
        var datetime = DateTime.Parse("2023/02/04 5:00:00 AM");

        // Act
        var result = datetime.ToShortReadableDate();

        // Assert
        result.Should().Be("4 Feb 2023");
    }

    [Test]
    public void ToShortReadableWithShortYearDate_ReturnsCorrectDateString_WhenGivenADateTime()
    {
        // Arrange
        var datetime = DateTime.Parse("2023/02/04 5:00:00 AM");

        // Act
        var result = datetime.ToShortReadableWithShortYearDate();

        // Assert
        result.Should().Be("4 Feb 23");
    }

    [Test]
    public void UtcToGmt_ReturnsCorrectDateTime_WhenDayLightSavingOff()
    {
        // Arrange
        var utcTime = new DateTime(2023, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var expectedGmtTime = new DateTime(2023, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = utcTime.UtcToGmt();

        // Assert
        result.Should().Be(expectedGmtTime);
    }

    [Test]
    public void UtcToGmt_ReturnsCorrectDateTime_WhenDayLightSavingOn()
    {
        // Arrange
        var utcTime = new DateTime(2023, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var expectedGmtTime = new DateTime(2023, 7, 15, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = utcTime.UtcToGmt();

        // Assert
        result.Should().Be(expectedGmtTime);
    }
}
