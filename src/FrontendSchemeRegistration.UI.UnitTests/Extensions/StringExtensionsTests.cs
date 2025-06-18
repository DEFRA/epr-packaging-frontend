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

        
        var actual = periodString.ToStartEndDate();

        // Assert
        actual.Start.Should().Be(expectedStart);
        actual.End.Should().Be(expectedEnd);
    }

    [Test]
    public void AppendBackLink_ShouldReturnBasePath_WhenNoFlagsSet()
    {
        //Arrange
        var basePath = "test/path";

        // Act
        var result = basePath.AppendBackLink(false, null);

        // Assert
        result.Should().Be(basePath);
    }

    [Test]
    public void AppendBackLink_ShouldIncludeIsResubmission_WhenSetTrue()
    {
        //Arrange
        var basePath = "test/path";

        // Act
        var result = basePath.AppendBackLink(true, null);

        //Assert
        result.Should().Contain("isResubmission=true");
    }

    [Test]
    public void AppendBackLink_ShouldIncludeRegistrationYear_WhenProvided()
    {
        //Arrange
        var basePath = "test/path";

        //Act
        var result = basePath.AppendBackLink(false, 2024);

        //Assert
        result.Should().Contain("registrationyear=2024");
    }

    [Test]
    public void AppendBackLink_ShouldIncludeBothParams_WhenBothSet()
    {
        //Arrange
        var basePath = "test/path";

        //Act
        var result = basePath.AppendBackLink(true, 2024);

        //Assert
        result.Should().Contain("isResubmission=true")
                     .And.Contain("registrationyear=2024");
    }

    [Test]
    public void AppendResubmissionFlagToQueryString_ShouldReturnLink_WhenParametersAreNull()
    {
        //Arrange
        string link = "/test";
        IDictionary<string, string> parameters = null;

        //Act
        var result = link.AppendResubmissionFlagToQueryString(parameters);

        //Assert
        result.Should().Be("/test");
    }

    [Test]
    public void AppendResubmissionFlagToQueryString_ShouldReturnLink_WhenParametersAreEmpty()
    {
        //Arrange
        string link = "/test";
        var parameters = new Dictionary<string, string>();

        //Act
        var result = link.AppendResubmissionFlagToQueryString(parameters);

        //Assert
        result.Should().Be("/test");
    }

    [Test]
    public void AppendResubmissionFlagToQueryString_ShouldAppendQueryParams_WhenParametersAreProvided()
    {
        //Arrange
        string link = "/test";
        var parameters = new Dictionary<string, string>
        {
            { "isResubmission", "true" },
            { "registrationyear", "2024" }
        };

        //Act
        var result = link.AppendResubmissionFlagToQueryString(parameters);

        //Assert
        result.Should().Contain("/test?")
              .And.Contain("isResubmission=true")
              .And.Contain("registrationyear=2024");
    }
}