namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UI.Extensions;

[TestFixture]
public class ModelStateDictionaryExtensionTests
{
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }

    [Test]
    public void ToErrorDictionary_Should_Return_Dictionary_Of_Errors()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Key1", "Error1");
        modelState.AddModelError("Key2", "Error2");
        modelState.AddModelError("Key2", "Error3");

        // Act
        var result = modelState.ToErrorDictionary();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.Should().ContainKey("Key1");
        result.Should().ContainKey("Key2");
        result["Key1"].Should().HaveCount(1);
        result["Key1"][0].Message.Should().Be("Error1");
        result["Key2"].Should().HaveCount(2);
        result["Key2"][0].Message.Should().Be("Error2");
        result["Key2"][1].Message.Should().Be("Error3");
    }
}