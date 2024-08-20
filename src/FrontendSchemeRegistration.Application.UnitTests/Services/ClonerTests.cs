namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using FluentAssertions;

[TestFixture]
public class ClonerTests
{
    private readonly Cloner _systemUnderTest;

    public ClonerTests()
    {
        _systemUnderTest = new Cloner();
    }

    [Test]
    public async Task Clone_WhenCalled_ReturnsCorrectResult()
    {
        // Arrange
        var original = new { SomeField = "SomeValue" };

        // Act
        var clone = _systemUnderTest.Clone(original);

        // Assert
        clone.SomeField.Should().BeEquivalentTo(original.SomeField);
        clone.Should().NotBeSameAs(original);
    }
}