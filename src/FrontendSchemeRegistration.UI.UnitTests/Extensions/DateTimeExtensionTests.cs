namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using System.Globalization;
using FluentAssertions;
using UI.Extensions;

[TestFixture]
public class DateTimeExtensionTests
{
    [Test]
    public void FormatWithCulture_ReturnsCorrectDateString_WhenGivenADateTime()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 5, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.FormatWithCulture();

            // Assert
            result.Should().Be("4 May 2023");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void FormatWithCulture_RespectsCultureInfo_WhenCultureChanges()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 5, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            // Act
            var result = datetime.FormatWithCulture();

            // Assert
            result.Should().Be("4 mai 2023");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void FormatPreviousDayWithCulture_ReturnsPreviousDayFormatted_WhenGivenADateTime()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 5, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.FormatPreviousDayWithCulture();

            // Assert
            result.Should().Be("3 May 2023");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void FormatPreviousDayWithCulture_HandlesMonthBoundary_WhenDateIsFirstOfMonth()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 1, 5, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.FormatPreviousDayWithCulture();

            // Assert
            result.Should().Be("30 April 2023");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void FormatPreviousDayWithCulture_HandlesYearBoundary_WhenDateIsFirstOfYear()
    {
        // Arrange
        var datetime = new DateTime(2023, 1, 1, 5, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.FormatPreviousDayWithCulture();

            // Assert
            result.Should().Be("31 December 2022");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void PreviousMinuteWithCulture_ReturnsPreviousMinuteFormatted_WhenGivenADateTime()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 14, 30, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.PreviousMinuteWithCulture();

            // Assert
            result.Should().Be("02:29pm");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void PreviousMinuteWithCulture_HandlesHourBoundary_WhenMinuteIsZero()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 14, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.PreviousMinuteWithCulture();

            // Assert
            result.Should().Be("01:59pm");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void PreviousMinuteWithCulture_HandlesNoonToAm_WhenCrossingMidday()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 12, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.PreviousMinuteWithCulture();

            // Assert
            result.Should().Be("11:59am");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void PreviousMinuteWithCulture_HandlesMidnightBoundary_WhenTimeIsJustAfterMidnight()
    {
        // Arrange
        var datetime = new DateTime(2023, 5, 4, 0, 0, 0);
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        try
        {
            // Act
            var result = datetime.PreviousMinuteWithCulture();

            // Assert
            result.Should().Be("11:59pm");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

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
