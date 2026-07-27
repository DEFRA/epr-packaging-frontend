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

    [TestCase(0, 2026)]
    [TestCase(-1, 2026)]
    [TestCase(1, 2027)]
    public void GetCsocSubmissionDeadline_WhenNow_ShouldBeExpected(int offset, int expectedYear)
    {
        var now = new DateTime(2026, 1, 31, 0,0,0, DateTimeKind.Unspecified).AddMilliseconds(offset);

        now.GetCsocSubmissionDeadline().Should().Be(new DateTime(expectedYear, 1, 31, 0,0,0, DateTimeKind.Unspecified));
    }

    [TestCase(2026, 12, 1, 2026, true)]
    [TestCase(2026, 12, 31, 2026, true)]
    [TestCase(2027, 1, 1, 2026, true)]
    [TestCase(2027, 1, 31, 2026, true)]
    [TestCase(2026, 11, 30, 2026, false)]
    [TestCase(2027, 2, 1, 2026, false)]
    [TestCase(2025, 12, 15, 2026, false)]
    [TestCase(2026, 12, 15, 2025, false)]
    [TestCase(2027, 1, 15, 2025, false)]
    [TestCase(2027, 12, 15, 2026, false)]
    public void IsInDecemberWasteFlashWindow_Returns_Expected(
        int year, int month, int day, int obligationYear, bool expected)
    {
        var now = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero);

        now.IsInDecemberWasteFlashWindow(obligationYear).Should().Be(expected);
    }
}