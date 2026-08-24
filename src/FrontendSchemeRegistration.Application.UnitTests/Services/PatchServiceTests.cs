namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using DTOs;
using FluentAssertions;

[TestFixture]
public class PatchServiceTests
{
    private readonly PatchService _systemUnderTest;

    public PatchServiceTests()
    {
        _systemUnderTest = new PatchService();
    }

    [Test]
    public async Task CreatePatchDocument_WhenObjectsAddedAndUpdatedInArray_ReturnCorrectPatchDocument()
    {
        // Arrange
        var originalObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                },
            },
        };

        var modifiedObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                    DeclarationPolicyAccepted = true,
                },
                new ()
                {
                    PrivacyPolicyAccepted = true,
                    DeclarationPolicyAccepted = true,
                },
            },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Should().NotBeNull();
        result.Operations.Count.Should().Be(2);
        result.Operations[0].path.Should().Be("/Users/0/DeclarationPolicyAccepted");
        result.Operations[0].op.Should().Be("replace");
        result.Operations[1].path.Should().Be("/Users/-");
        result.Operations[1].op.Should().Be("add");
        originalObject.Users[0].DeclarationPolicyAccepted.Should().BeTrue();
        modifiedObject.Should().BeEquivalentTo(originalObject);
    }

    [Test]
    public async Task CreatePatchDocument_WhenObjectModified_ReturnCorrectPatchDocument()
    {
        // Arrange
        var originalObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                },
                new ()
                {
                    PrivacyPolicyAccepted = true,
                },
            },
        };

        var modifiedObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                },
                new ()
                {
                    PrivacyPolicyAccepted = true,
                    DeclarationPolicyAccepted = true,
                },
            },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Should().NotBeNull();
        result.Operations.Count.Should().Be(1);
        result.Operations[0].path.Should().Be("/Users/1/DeclarationPolicyAccepted");
        result.Operations[0].op.Should().Be("replace");
        originalObject.Users[1].DeclarationPolicyAccepted.Should().BeTrue();
        modifiedObject.Should().BeEquivalentTo(originalObject);
    }

    [Test]
    public async Task CreatePatchDocument_WhenObjectsIdentical_ReturnsEmptyPatchDocument()
    {
        // Arrange
        var originalObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                    DeclarationPolicyAccepted = true,
                },
            },
        };

        var modifiedObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                    DeclarationPolicyAccepted = true,
                },
            },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);

        // Assert
        result.Should().NotBeNull();
        result.Operations.Count.Should().Be(0);
    }

    [Test]
    public async Task CreatePatchDocument_WhenSameTypeScalarPropertyChanges_ReturnsReplaceOperation()
    {
        // Arrange
        var originalObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = true,
                },
            },
        };

        var modifiedObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                new ()
                {
                    PrivacyPolicyAccepted = false,
                },
            },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Operations.Count.Should().Be(1);
        result.Operations[0].path.Should().Be("/Users/0/PrivacyPolicyAccepted");
        result.Operations[0].op.Should().Be("replace");
        originalObject.Users[0].PrivacyPolicyAccepted.Should().BeFalse();
    }

    [Test]
    public async Task CreatePatchDocument_WhenArrayShrinks_ReturnsRemoveOperation()
    {
        // Arrange
        var unchangedUser = new UserDto { CustomerId = Guid.NewGuid(), PrivacyPolicyAccepted = true };
        var originalObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                unchangedUser,
                new () { PrivacyPolicyAccepted = true },
            },
        };

        var modifiedObject = new ApplicationDto
        {
            Users = new List<UserDto>
            {
                unchangedUser,
            },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Operations.Count.Should().Be(1);
        result.Operations[0].path.Should().Be("/Users/1");
        result.Operations[0].op.Should().Be("remove");
        originalObject.Users.Should().HaveCount(1);
    }

    [Test]
    public async Task CreatePatchDocument_WhenNestedObjectPropertyChanges_ReturnsNestedReplaceOperation()
    {
        // Arrange
        var originalObject = new NestedTestDto
        {
            Name = "Unchanged",
            Inner = new InnerTestDto { Value = "Old" },
        };

        var modifiedObject = new NestedTestDto
        {
            Name = "Unchanged",
            Inner = new InnerTestDto { Value = "New" },
        };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Operations.Count.Should().Be(1);
        result.Operations[0].path.Should().Be("/Inner/Value");
        result.Operations[0].op.Should().Be("replace");
        originalObject.Inner.Value.Should().Be("New");
    }

    [Test]
    public async Task CreatePatchDocument_WhenPrimitiveArrayElementChanges_ReturnsReplaceOperation()
    {
        // Arrange
        var originalObject = new PrimitiveListTestDto { Tags = new List<string> { "a", "b" } };
        var modifiedObject = new PrimitiveListTestDto { Tags = new List<string> { "a", "c" } };

        // Act
        var result = _systemUnderTest.CreatePatchDocument(originalObject, modifiedObject);
        result.ApplyTo(originalObject);

        // Assert
        result.Operations.Count.Should().Be(1);
        result.Operations[0].path.Should().Be("/Tags/1");
        result.Operations[0].op.Should().Be("replace");
        originalObject.Tags.Should().BeEquivalentTo(modifiedObject.Tags);
    }

    private class NestedTestDto
    {
        public string Name { get; set; }

        public InnerTestDto Inner { get; set; }
    }

    private class InnerTestDto
    {
        public string Value { get; set; }
    }

    private class PrimitiveListTestDto
    {
        public List<string> Tags { get; set; }
    }
}