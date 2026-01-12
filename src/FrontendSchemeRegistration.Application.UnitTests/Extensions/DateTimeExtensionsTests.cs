namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

using Application.Extensions;
using FluentAssertions;

[TestFixture]
public class DateTimeExtensionsTests
{
    [Test]
    [TestCase(2024, 1, 2024)]
    [TestCase(2025, 1, 2025)]
    [TestCase(2026, 1, 2025)] // Special case: January 2026 should return 2025
    [TestCase(2026, 2, 2026)] // Special case: February 2026 should return 2026
    [TestCase(2027, 1, 2027)]
    [TestCase(2028, 1, 2028)]
    [TestCase(2029, 1, 2029)]
    [TestCase(2030, 1, 2030)]
    public void GetComplianceYear_Returns_CorrectYear(int year, int month, int expectedComplianceYear)
    {
        // Arrange
        var date = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = date.GetComplianceYear();

        // Assert
        result.Should().Be(expectedComplianceYear);
    }

    [Test]
    public void GetComplianceYear_Returns_SameYear_ForNonJanuaryDates()
    {
        // Arrange
        var dates = new[]
        {
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 7, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 11, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        foreach (var date in dates)
            date.GetComplianceYear().Should().Be(date.Year,
                $"because {date:yyyy-MM-dd} is not in a special compliance period");
    }

    [Test]
    public void GetComplianceYear_Returns_PreviousYear_OnlyForJanuary2026()
    {
        // Arrange
        var january2026Dates = new[]
        {
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        foreach (var date in january2026Dates)
            date.GetComplianceYear().Should().Be(2025,
                $"because {date:yyyy-MM-dd} is in the special January 2026 period");
    }

    [Test]
    public void GetComplianceYear_Returns_SameYear_ForJanuaryDatesOtherThan2026()
    {
        // Arrange
        var otherJanuaryDates = new[]
        {
            new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        foreach (var date in otherJanuaryDates)
            date.GetComplianceYear().Should().Be(date.Year,
                $"because {date:yyyy-MM-dd} is not in January 2026");
    }
}