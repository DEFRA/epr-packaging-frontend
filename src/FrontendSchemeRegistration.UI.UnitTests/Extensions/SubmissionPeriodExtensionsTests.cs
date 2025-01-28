namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using UI.Enums;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Constants;

[TestFixture]
public class SubmissionPeriodExtensionsTests
{
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
    public void SubmissionPeriodId_ToReferenceNumberFormat_ShouldFormatCorrectly_For_Welsh(string submissionPeriod, MonthType? monthType, string expectedResult)
    {
        var input = new SubmissionPeriodId { SubmissionPeriod = submissionPeriod };
        var result = input.LocalisedMonth(monthType);
        result.Should().Be(expectedResult);
    }
}