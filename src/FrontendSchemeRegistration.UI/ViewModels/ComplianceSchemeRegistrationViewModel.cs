namespace FrontendSchemeRegistration.UI.ViewModels;

using Shared;

public record class ComplianceSchemeRegistrationViewModel(
    string ComplianceSchemeName,
    string Nation,
    IEnumerable<RegistrationYearApplicationsViewModel> RegistrationApplicationYears)
{
    public bool DisplayCsoSmallProducerRegistration { get; set; } = false;
};
