namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

using System.Globalization;
using Application.Extensions;
using FluentAssertions;

[TestFixture]
public class DateTimeExtensionsTests
{
    [Test]
    // The CSoC deadline is 31 January, when the UK is on GMT (UTC+0), so UTC and UK dates are the same
    [TestCase("2027-01-15T12:00:00Z", "2027-01-31", false)]
    [TestCase("2027-01-31T00:00:00Z", "2027-01-31", false)]
    [TestCase("2027-01-31T23:59:59Z", "2027-01-31", false)]
    [TestCase("2027-02-01T00:00:00Z", "2027-01-31", true)]
    [TestCase("2027-03-15T12:00:00Z", "2027-01-31", true)]
    public void DateTime_IsAfterUkDate_UsesUkCalendarDate(string utcNow, string date, bool expected)
    {
        // Arrange
        var now = DateTime.Parse(utcNow, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        var compareTo = DateTime.Parse(date, CultureInfo.InvariantCulture);

        // Act
        var result = now.IsAfterUkDate(compareTo);

        // Assert
        result.Should().Be(expected);
    }

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

    [TestCase("2026-11-30", 2026, false)]
    [TestCase("2026-12-01", 2026, true)]
    [TestCase("2026-12-31", 2026, true)]
    [TestCase("2027-01-01", 2026, true)]
    [TestCase("2027-01-31", 2026, true)]
    [TestCase("2027-02-01", 2026, false)]
    [TestCase("2026-06-15", 2026, false)]
    [TestCase("2026-12-15", 2025, false)]
    [TestCase("2027-01-12", 2025, false)]
    [TestCase("2026-12-15", 2027, false)]
    public void IsInDecemberJanuaryFlashWindow_ForAccreditationYear_Returns_Expected(
        string nowText, int accreditationYear, bool expected)
    {
        var now = DateTimeOffset.Parse(nowText + "T12:00:00Z", DateTimeFormatInfo.InvariantInfo);

        now.IsInDecemberJanuaryFlashWindow(accreditationYear).Should().Be(expected);
    }

    [Test]
    public void IsInDecemberJanuaryFlashWindow_ForAccreditationYear_UsesUkTime()
    {
        // 23:30 UTC on 31 Jan 2027 is still 31 Jan in the UK (GMT); 00:30 UTC on 1 Feb is not.
        new DateTimeOffset(2027, 1, 31, 23, 30, 0, TimeSpan.Zero).IsInDecemberJanuaryFlashWindow(2026).Should().BeTrue();
        new DateTimeOffset(2027, 2, 1, 0, 30, 0, TimeSpan.Zero).IsInDecemberJanuaryFlashWindow(2026).Should().BeFalse();
    }

    [TestCase("2025-11-30", false)]
    [TestCase("2025-12-01", true)]
    [TestCase("2025-12-31", true)]
    [TestCase("2026-01-01", true)]
    [TestCase("2026-01-31", true)]
    [TestCase("2026-02-01", false)]
    [TestCase("2026-06-15", false)]
    public void IsInDecemberJanuaryFlashWindow_Returns_Expected(string nowText, bool expected)
    {
        var now = DateTimeOffset.Parse(nowText + "T12:00:00Z", DateTimeFormatInfo.InvariantInfo);

        now.IsInDecemberJanuaryFlashWindow().Should().Be(expected);
    }

    [TestCase("2026-01-30T23:59:59", 2026)]
    [TestCase("2026-01-31T00:00:00", 2026)]
    [TestCase("2026-01-31T12:00:00", 2026)]
    [TestCase("2026-01-31T23:59:59.999", 2026)]
    [TestCase("2026-02-01T00:00:00", 2027)]
    [TestCase("2026-11-15T12:00:00", 2027)]
    public void GetCsocSubmissionDeadline_WhenNow_ShouldBeExpected(string nowText, int expectedYear)
    {
        var now = DateTime.Parse(nowText, CultureInfo.InvariantCulture);

        now.GetCsocSubmissionDeadline().Should().Be(new DateTime(expectedYear, 1, 31, 0, 0, 0, DateTimeKind.Unspecified));
    }
}
