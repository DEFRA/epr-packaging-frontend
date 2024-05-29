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
    [TestCase("January", "2024", "Start", "January")]
    [TestCase("June", "2024", "End", "June")]
    [TestCase("July", "2024", "Start", "July")]
    [TestCase("December", "2024", "End", "December")]
    public void SubmissionPeriod_ToReferenceNumberFormat_ShouldFormatCorrectly_For_English(string month, string year, string monthType, string expectedResult)
    {
        var input = new SubmissionPeriod { StartMonth = month, EndMonth = month, Year = year };
        Enum.TryParse<MonthType>(monthType, out var parseMonthType);
        var result = input.LocalisedMonth(parseMonthType);
        result.Should().Be(expectedResult);
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
        Enum.TryParse<MonthType>(monthType, out var parseMonthType);
        var result = input.LocalisedMonth(parseMonthType);
        result.Should().Be(expectedResult);
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
        Enum.TryParse<MonthType>(monthType, out var parseMonthType);
        var result = input.LocalisedMonth(parseMonthType);
        result.Should().Be(expectedResult);
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
        Enum.TryParse<MonthType>(monthType, out var parseMonthType);
        var result = input.LocalisedMonth(parseMonthType);
        result.Should().Be(expectedResult);
    }
}