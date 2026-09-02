namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

using Application.Enums;
using Application.Extensions;
using FluentAssertions;

[TestFixture]
public class NationExtensionsTests
{
    [TestCase("GB-ENG", "England")]
    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-NIR", "NorthernIreland")]
    [TestCase("GB-WLS", "Wales")]
    [TestCase("GB-XXX", "")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void GetNationName_Returns_ExpectedName(string nationCode, string expected)
    {
        NationExtensions.GetNationName(nationCode).Should().Be(expected);
    }

    [TestCase((int)Nation.England, "GB-ENG")]
    [TestCase((int)Nation.Scotland, "GB-SCT")]
    [TestCase((int)Nation.Wales, "GB-WLS")]
    [TestCase((int)Nation.NorthernIreland, "GB-NIR")]
    [TestCase((int)Nation.NotSet, "")]
    [TestCase(999, "")]
    public void GetNationNameFromId_Returns_ExpectedCode(int nationId, string expected)
    {
        NationExtensions.GetNationNameFromId(nationId).Should().Be(expected);
    }

    [TestCase("GB-ENG", "Environment Agency")]
    [TestCase("GB-SCT", "Scottish Environment Protection Agency")]
    [TestCase("GB-NIR", "Northern Ireland Environment Agency")]
    [TestCase("GB-WLS", "Natural Resources Wales")]
    [TestCase("GB-XXX", "")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void GetEnvironmentAgencyName_Returns_ExpectedName(string nationName, string expected)
    {
        NationExtensions.GetEnvironmentAgencyName(nationName).Should().Be(expected);
    }

    [TestCase((int)Nation.NorthernIreland, "packaging@daera-ni.gov.uk")]
    [TestCase((int)Nation.Scotland, "producer.responsibility@sepa.org.uk")]
    [TestCase((int)Nation.Wales, "packaging@naturalresourceswales.gov.uk")]
    [TestCase((int)Nation.England, "packagingproducers@environment-agency.gov.uk")]
    [TestCase((int)Nation.NotSet, "packagingproducers@environment-agency.gov.uk")]
    [TestCase(999, "packagingproducers@environment-agency.gov.uk")]
    public void GetEnvironmentAgencyEmailLink_Returns_ExpectedMailToLink(int nationId, string expectedEmail)
    {
        var result = NationExtensions.GetEnvironmentAgencyEmailLink(nationId);

        result.ToString().Should().Be(
            $"<a class=\"govuk-link govuk-link--no-visited-state\" href=\"mailto:{expectedEmail}\">{expectedEmail}</a>");
    }
}
