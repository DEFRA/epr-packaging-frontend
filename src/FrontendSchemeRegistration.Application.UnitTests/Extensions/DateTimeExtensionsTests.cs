namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

using Application.Extensions;
using FluentAssertions;

[TestFixture]
public class DateTimeExtensionsTests
{
    [Test]
    [TestCase(2024, 1, 1, 2023)]
    [TestCase(2024, 2, 1, 2024)]
    [TestCase(2024, 12, 31, 2024)]
    [TestCase(2025, 1, 1, 2024)]
    [TestCase(2025, 2, 1, 2025)]
    [TestCase(2025, 12, 31, 2025)]
    [TestCase(2026, 1, 1, 2025)]
    [TestCase(2026, 2, 1, 2026)]
    [TestCase(2026, 12, 31, 2026)]
    public void DateTime_GetComplianceYear_Returns_CorrectYear(int year, int month, int day, int expectedComplianceYear)
    {
        // Arrange
        var date = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = date.GetComplianceYear();

        // Assert
        result.Should().Be(expectedComplianceYear);
    }

    [Test]
    // No offset
    [TestCase(2025, 12, 1, 0, 2025)]
    [TestCase(2025, 12, 31, 0, 2025)]
    [TestCase(2026, 1, 1, 0, 2025)]
    [TestCase(2026, 1, 31, 0, 2025)]
    [TestCase(2026, 2, 1, 0, 2026)]
    [TestCase(2026, 2, 2, 0, 2026)]

    // +14hrs
    [TestCase(2025, 12, 1, 14, 2025)]
    [TestCase(2025, 12, 31, 14, 2025)]
    [TestCase(2026, 1, 1, 14, 2025)]
    [TestCase(2026, 1, 31, 14, 2025)]
    [TestCase(2026, 2, 1, 14, 2025)]
    [TestCase(2026, 2, 2, 14, 2026)]

    // -14hrs
    [TestCase(2025, 12, 1, -14, 2025)]
    [TestCase(2025, 12, 31, -14, 2025)]
    [TestCase(2026, 1, 1, -14, 2025)]
    [TestCase(2026, 1, 31, -14, 2026)]
    [TestCase(2026, 2, 1, -14, 2026)]
    [TestCase(2026, 2, 2, -14, 2026)]
    public void DateTimeOffset_GetComplianceYear_Returns_CorrectYear(int year, int month, int day, int offsetHours,
        int expectedComplianceYear)
    {
        // Arrange
        var offset = new DateTimeOffset(year, month, day, 12, 0, 0, new TimeSpan(offsetHours, 0, 0));

        // Act
        var result = offset.GetComplianceYear();

        // Assert
        result.Should().Be(expectedComplianceYear);
    }
}