namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels;

using FrontendSchemeRegistration.UI.ViewModels;

public class ComplianceSchemeLandingViewModelTests
{
    [TestCase("", 0)]
    [TestCase("2025", 2025)]
    [TestCase("2026", 2026)]
    [TestCase("2027", 2027)]
    [TestCase("a2028", 0)]
    [TestCase("jkdjkf", 0)]
    public async Task ComplianceSchemeLandingViewModel_ConvertsYearToIntAndHandlesFailedParsings(string complianceYear, int yearAsInt)
    {
        var model = new ComplianceSchemeLandingViewModel
        {
            ComplianceYear = complianceYear
        };
        Assert.That(model.ComplianceYearAsInteger(complianceYear), Is.EqualTo(yearAsInt));
    }
}