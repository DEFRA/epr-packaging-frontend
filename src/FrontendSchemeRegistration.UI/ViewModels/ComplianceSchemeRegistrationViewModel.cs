namespace FrontendSchemeRegistration.UI.ViewModels;

using Shared;

public record class ComplianceSchemeRegistrationViewModel(
    string ComplianceSchemeName,
    string Nation,
    IEnumerable<RegistrationYearApplicationsViewModel> RegistrationApplicationYears,
    IEnumerable<RegistrationApplicationViewModel> LegacyRegistrationApplications,
    int CurrentRegistrationYear)
{
    public int PreviousRegistrationYear { get; } = CurrentRegistrationYear - 1;
    public int LegacyPreviousRegistrationYear { get; } = CurrentRegistrationYear - 2;
    public bool DisplayCsoSmallProducerRegistration { get; set; } = false;
};
