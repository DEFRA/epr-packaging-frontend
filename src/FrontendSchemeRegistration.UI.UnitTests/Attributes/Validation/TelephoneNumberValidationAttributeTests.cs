namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.Validation;

using FluentAssertions;
using UI.Attributes.Validation;

[TestFixture]
public class TelephoneNumberValidationAttributeTests
{
    private readonly TelephoneNumberValidationAttribute _telephoneNumberValidationAttribute = new();

    [Test]
    [TestCase("020 1212 1212", true)]
    [TestCase(null, false)]
    public void IsValid_WhenPassedValidNumber_ReturnsTrue(string? number, bool expectedResult)
    {
        // Act
        var result = _telephoneNumberValidationAttribute.IsValid(number);

        // Assert
        result.Should().Be(expectedResult);
    }
}