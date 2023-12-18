namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.Validation;

using FluentAssertions;
using UI.Attributes.Validation;

[TestFixture]
public class MustTrueTests
{
    private readonly MustTrueAttribute _mustTrueAttribute = new MustTrueAttribute();

    [Test]
    public void IsValid_WhenPassedTrue_ReturnsTrue()
    {
        // Arrange
        const bool value = true;

        // Act
        var result = _mustTrueAttribute.IsValid(value);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValid_WhenPassedFalse_ReturnsFalse()
    {
        // Arrange
        const bool value = false;

        // Act
        var result = _mustTrueAttribute.IsValid(value);

        // Assert
        result.Should().BeFalse();
    }
}