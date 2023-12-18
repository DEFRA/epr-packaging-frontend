namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using UI.Extensions;

[TestFixture]
public class StringExtensionsTests
{
    [Test]
    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("123456", "123 456")]
    [TestCase("1 2 3 4 5 6", "123 456")]
    [TestCase("1234567", "1 234 567")]
    public void ToReferenceNumberFormat_ShouldFormatCorrectly(string input, string expectedResult)
    {
        var result = input.ToReferenceNumberFormat();

        result.Should().Be(expectedResult);
    }

    [Test]
    public void ToStartEndDate_ShouldReturnCorrectDates_GivenValidPeriodString()
    {
        // Arrange
        var periodString = "Jan to June 2023";
        var expectedStart = new DateTime(2023, 01, 01);
        var expectedEnd = new DateTime(2023, 06, 30);

        // Act
        var actual = periodString.ToStartEndDate();

        // Assert
        actual.Start.Should().Be(expectedStart);
        actual.End.Should().Be(expectedEnd);
    }

    [Test]
    public void ToStartEndDate_ShouldMinDates_GivenInvalidPeriodString()
    {
        // Arrange
        var periodString = "NOT A DATE";
        var expectedStart = DateTime.MinValue;
        var expectedEnd = DateTime.MinValue;

        // Act
        var actual = periodString.ToStartEndDate();

        // Assert
        actual.Start.Should().Be(expectedStart);
        actual.End.Should().Be(expectedEnd);
    }
}