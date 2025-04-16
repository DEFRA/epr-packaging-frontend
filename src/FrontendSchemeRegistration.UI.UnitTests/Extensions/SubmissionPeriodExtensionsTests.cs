using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using System.Globalization;

namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

[TestFixture]
public class SubmissionPeriodExtensionsTests
{
    private List<SubmissionPeriod> _submissionPeriods = new()
    {
        new()
        {
            DataPeriod = "January to June 2023",
            StartMonth = "January",
            EndMonth = "June",
            Year = "2023"
        },
        new()
        {
            DataPeriod = "July to December 2023",
            StartMonth = "July",
            EndMonth = "December",
            Year = "2023"
        },
        new()
        {
            DataPeriod = "January to June 2024",
            StartMonth = "January",
            EndMonth = "June",
            Year = "2024"
        },
        new()
        {
            DataPeriod = "July to December 2024",
            StartMonth = "July",
            EndMonth = "December",
            Year = "2024"
        },
        new()
        {
            DataPeriod = "January to June 2025",
            StartMonth = "January",
            EndMonth = "June",
            Year = "2025"
        },
        new()
        {
            DataPeriod = "July to December 2025",
            StartMonth = "July",
            EndMonth = "December",
            Year = "2025"
        },
    };

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, null, "")]
    [TestCase("January", "2024", "", "")]
    [TestCase("January", "2024", "Start", "January")]
    [TestCase("June", "2024", "End", "June")]
    [TestCase("July", "2024", "Start", "July")]
    [TestCase("December", "2024", "End", "December")]
    public void SubmissionPeriod_ToReferenceNumberFormat_ShouldFormatCorrectly_For_English(string month, string year, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.Welsh)]
    [SetCulture(Language.Welsh)]
    [TestCase("January", "2024", "Start", "Ionawr")]
    [TestCase("June", "2024", "End", "Fehefin")]
    [TestCase("July", "2024", "Start", "Gorffennaf")]
    [TestCase("December", "2024", "End", "Ragfyr")]
    public void SubmissionPeriod_ToReferenceNumberFormat_ShouldFormatCorrectly_For_Welsh(string month, string year, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, "")]
    [TestCase("January to June 2024", "Start", "January")]
    [TestCase("January to June 2024", "End", "June")]
    [TestCase("July to December 2024", "Start", "July")]
    [TestCase("July to December 2024", "End", "December")]
    public void SubmissionPeriodId_ToReferenceNumberFormat_ShouldFormatCorrectly_For_English(string submissionPeriod, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriodId { SubmissionPeriod = submissionPeriod };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.Welsh)]
    [SetCulture(Language.Welsh)]
    [TestCase(null, null, "")]
    [TestCase("January to June 2024", "Start", "Ionawr")]
    [TestCase("January to June 2024", "End", "Fehefin")]
    [TestCase("July to December 2024", "Start", "Gorffennaf")]
    [TestCase("July to December 2024", "End", "Ragfyr")]
    public void SubmissionPeriodId_ToReferenceNumberFormat_ShouldFormatCorrectly_For_Welsh(string submissionPeriod, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriodId { SubmissionPeriod = submissionPeriod };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, null, "")]
    [TestCase("January", "2024", "Start", "Jan")]
    [TestCase("June", "2024", "End", "Jun")]
    [TestCase("July", "2024", "Start", "Jul")]
    [TestCase("December", "2024", "End", "Dec")]
    public void SubmissionPeriod_ToShortMonth_ShouldFormatCorrectly_For_English(string month, string year, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedShortMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.Welsh)]
    [SetCulture(Language.Welsh)]
    [TestCase("January", "2024", "Start", "Ion")]
    [TestCase("June", "2024", "End", "Meh")]
    [TestCase("July", "2024", "Start", "Gor")]
    [TestCase("December", "2024", "End", "Rhag")]
    public void SubmissionPeriod_ToShortMonth_ShouldFormatCorrectly_For_Welsh(string month, string year, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };

        if (Enum.TryParse<MonthType>(monthType, out var parseMonthType))
        {
            var result = input.LocalisedShortMonth(parseMonthType);
            result.Should().Be(expectedResult);
        }
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, null, "")]
    [TestCase("", "2024", MonthType.Start, "")]
    [TestCase("", "2024", MonthType.End, "")]
    [TestCase("December", "2024", MonthType.None, "")]
    public void SubmissionPeriod_LocalisedMonth_ShouldReturnEmptyForNull(string month, string year, MonthType? monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };
        var result = input.LocalisedMonth(monthType);
        result.Should().Be(expectedResult);
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, null, "")]
    [TestCase("", "2024", MonthType.Start, "")]
    [TestCase("", "2024", MonthType.End, "")]
    [TestCase("December", "2024", MonthType.None, "")]
    public void SubmissionPeriod_LocalisedShortMonth_ShouldReturnEmptyForNull(string month, string year, MonthType? monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };
        var result = input.LocalisedShortMonth(monthType);
        result.Should().Be(expectedResult);
    }

    [Test]
    [SetUICulture(Language.English)]
    [SetCulture(Language.English)]
    [TestCase(null, null, "")]
    [TestCase("January to June 2024", null, "")]
    [TestCase("", MonthType.Start, "")]
    [TestCase("", MonthType.End, "")]
    public void SubmissionPeriodId_ToLocalisedMonth_ShouldFormatCorrectly_For_English(string submissionPeriod, MonthType? monthType, string expectedResult)
    {
        var input = new SubmissionPeriodId { SubmissionPeriod = submissionPeriod };
        var result = input.LocalisedMonth(monthType);
        result.Should().Be(expectedResult);
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase(null, null, null, null)]
    [TestCase("January", "June", "2024", "2024-06-30")]
    [TestCase("July", "December", "2024", "2024-12-31")]
    public void SubmissionPeriod_GetEndDate_ShouldReturnExpected_Result(string startMonth, string endMonth, string year, string expectedResult)
    {
        DateOnly expectedDate = DateOnly.TryParse(expectedResult, new CultureInfo("en-GB"), out var date)
            ? date
            : DateOnly.MaxValue;

        var input = new SubmissionPeriod { StartMonth = startMonth, EndMonth = endMonth, Year = year };
        var result = input.GetEndDate();
        result.Should().Be(expectedDate);
    }

    [Test]
    public void SubmissionPeriodList_FilterToLatestAllowedPeriodEndDate_ShouldReturnFilteredList()
    {
        var endDate = new DateOnly(2024, 06, 30);

        var result = _submissionPeriods
            .FilterToLatestAllowedPeriodEndDate(endDate)
            ?.ToList();

        result.Should().NotBeNull();
        result.Count.Should().Be(3);

        result.Should().NotContain(p => p.DataPeriod == "July to December 2024");
        result.Should().NotContain(p => p.Year == "2025");
    }

    [Test]
    public void SubmissionPeriodList_FilterToLatestAllowedPeriodEndDate_ShouldReturnFilteredList_WithFullYear()
    {
        var finalSubmissionPeriod = new SubmissionPeriod
        {
            DataPeriod = "July to December 2024",
            StartMonth = "July",
            EndMonth = "December",
            Year = "2024"
        };
        var endDate = new DateOnly(2024, 12, 31);

        var result = _submissionPeriods
            .FilterToLatestAllowedPeriodEndDate(endDate)
            ?.ToList();

        result.Should().NotBeNull();
        result.Count.Should().Be(4);
        result.Should().NotContain(p => p.Year == "2025");
    }

    [Test]
    public void SubmissionPeriodList_FilterToLatestAllowedPeriodEndDate_ShouldReturnOriginal_List_WhenFinalPeriodIsLastInList()
    {
        var endDate = new DateOnly(2025, 12, 31);
        var result = _submissionPeriods.FilterToLatestAllowedPeriodEndDate(endDate);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(_submissionPeriods);
    }

    [Test]
    public void SubmissionPeriodList_FilterToLatestAllowedPeriodEndDate_ShouldReturnOriginalList_WhenFinalPeriodIsMaxValue()
    {
        var result = _submissionPeriods.FilterToLatestAllowedPeriodEndDate(DateOnly.MaxValue);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(_submissionPeriods);
    }

    [Test]
    [SetUICulture(Language.English)]
    [TestCase("January", "June", "2023", 2023, true)]
    [TestCase("January", "June", "2023", 2024, false)]
    [TestCase("July", "December", "2023", 2024, false)]
    [TestCase("January", "March", "2024", 2024, false)]
    [TestCase("January", "June", "2024", 2024, true)]
    [TestCase("July", "December", "2024", 2024, false)]
    [TestCase("January", "June", "2025", 2024, true)]
    [TestCase("July", "December", "2025", 2024, false)]
    [TestCase("January", "June", "2026", 2024, true)]
    public void IsJanuaryToJunePeriodFromYearOrLater_ShouldReturnCorrectResult(string startMonth, string endMonth, string year, int fromYear, bool expectedResult)
    {
        var periodDetail = new SubmissionPeriodDetail
        {
            DataPeriod = $"{startMonth} to {endMonth} {year}",
            DatePeriodStartMonth = startMonth,
            DatePeriodEndMonth = endMonth,
            DatePeriodYear = year
        };

        var result = periodDetail.IsJanuaryToJunePeriodFromYearOrLater(fromYear);
        result.Should().Be(expectedResult);
    }
}