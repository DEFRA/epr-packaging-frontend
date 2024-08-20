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
}