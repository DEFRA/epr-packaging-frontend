namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using Application.Services.Interfaces;
using FluentAssertions;

[TestFixture]
public class ClonerTests
{
    private readonly ICloner _systemUnderTest;

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