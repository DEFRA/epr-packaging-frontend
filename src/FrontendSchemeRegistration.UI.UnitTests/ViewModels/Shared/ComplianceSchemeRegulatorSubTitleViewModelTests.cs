namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Shared;

using FluentAssertions;
using UI.ViewModels.Shared;

[TestFixture]
public class ComplianceSchemeRegulatorSubTitleViewModelTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var systemUnderTest = new ComplianceSchemeRegulatorSubTitleViewModel("Compliance Scheme Ltd", "England");

        systemUnderTest.ComplianceSchemeName.Should().Be("Compliance Scheme Ltd");
        systemUnderTest.Nation.Should().Be("England");
    }

    [Test]
    public void Equals_WhenPropertiesMatch_ReturnsTrue()
    {
        var first = new ComplianceSchemeRegulatorSubTitleViewModel("Compliance Scheme Ltd", "England");
        var second = new ComplianceSchemeRegulatorSubTitleViewModel("Compliance Scheme Ltd", "England");

        first.Should().Be(second);
    }

    [Test]
    public void Equals_WhenPropertiesDiffer_ReturnsFalse()
    {
        var first = new ComplianceSchemeRegulatorSubTitleViewModel("Compliance Scheme Ltd", "England");
        var second = new ComplianceSchemeRegulatorSubTitleViewModel("Compliance Scheme Ltd", "Wales");

        first.Should().NotBe(second);
    }
}
