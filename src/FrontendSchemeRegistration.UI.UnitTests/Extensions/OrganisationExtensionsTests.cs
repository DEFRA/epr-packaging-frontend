namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using UI.Extensions;

[TestFixture]
public class OrganisationExtensionsTests
{
    [TestCase("SomethingElse", false)]
    [TestCase(OrganisationRoles.Producer, true)]
    public void IsDirectProducer_ShouldBeAsExpected(string role, bool isDirectProducer)
    {
        var subject = new Organisation
        {
            OrganisationRole = role
        };

        subject.IsDirectProducer().Should().Be(isDirectProducer);
    }
    
    [TestCase("SomethingElse", false)]
    [TestCase(OrganisationRoles.ComplianceScheme, true)]
    public void IsComplianceScheme_ShouldBeAsExpected(string role, bool isComplianceScheme)
    {
        var subject = new Organisation
        {
            OrganisationRole = role
        };

        subject.IsComplianceScheme().Should().Be(isComplianceScheme);
    }
}