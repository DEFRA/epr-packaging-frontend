namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.DTOs.ComplianceScheme;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using UI.Controllers;

[TestFixture]
public class SetupMinimalSessionTests
{
    [Test]
    public void FrontendSchemeRegistrationSession_WithComplianceSchemes_SelectedComplianceSchemeIdIsNull_UsesFirstComplianceScheme()
    {
        // Arrange
        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "First CS" },
            new() { Id = Guid.NewGuid(), Name = "Second CS" }
        };
        var userData = new UserData { Id = Guid.NewGuid() };

        // Act
        var result = SetupMinimalSession.FrontendSchemeRegistrationSession(complianceSchemes, userData, null);

        // Assert
        result.RegistrationSession.SelectedComplianceScheme.Should().Be(complianceSchemes[0]);
    }

    [Test]
    public void FrontendSchemeRegistrationSession_WithComplianceSchemes_SelectedComplianceSchemeIdMatchesScheme_UsesMatchingComplianceScheme()
    {
        // Arrange
        var matchingId = Guid.NewGuid();
        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "First CS" },
            new() { Id = matchingId, Name = "Second CS" }
        };
        var userData = new UserData { Id = Guid.NewGuid() };

        // Act
        var result = SetupMinimalSession.FrontendSchemeRegistrationSession(complianceSchemes, userData, matchingId);

        // Assert
        result.RegistrationSession.SelectedComplianceScheme.Should().Be(complianceSchemes[1]);
    }

    [Test]
    public void FrontendSchemeRegistrationSession_WithComplianceSchemes_SelectedComplianceSchemeIdDoesNotMatchAnyScheme_SelectedComplianceSchemeIsNull()
    {
        // Arrange
        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "First CS" },
            new() { Id = Guid.NewGuid(), Name = "Second CS" }
        };
        var userData = new UserData { Id = Guid.NewGuid() };

        // Act
        var result = SetupMinimalSession.FrontendSchemeRegistrationSession(complianceSchemes, userData, Guid.NewGuid());

        // Assert
        result.RegistrationSession.SelectedComplianceScheme.Should().BeNull();
    }

    [Test]
    public void FrontendSchemeRegistrationSession_WithProducerComplianceScheme_SetsCurrentComplianceScheme()
    {
        // Arrange
        var producerComplianceSchemeDto = new ProducerComplianceSchemeDto
        {
            SelectedSchemeId = Guid.NewGuid(),
            ComplianceSchemeId = Guid.NewGuid(),
            ComplianceSchemeName = "Test CS"
        };
        var userData = new UserData { Id = Guid.NewGuid() };

        // Act
        var result = SetupMinimalSession.FrontendSchemeRegistrationSession(producerComplianceSchemeDto, userData);

        // Assert
        result.RegistrationSession.CurrentComplianceScheme.Should().Be(producerComplianceSchemeDto);
        result.UserData.Should().Be(userData);
    }
}
