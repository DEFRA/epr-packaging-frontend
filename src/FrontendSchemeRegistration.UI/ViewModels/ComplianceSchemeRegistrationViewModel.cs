namespace FrontendSchemeRegistration.UI.ViewModels;

public record class ComplianceSchemeRegistrationViewModel(
    string ComplianceSchemeName,
    string Nation,
    IEnumerable<RegistrationApplicationPerYearViewModel> RegistrationApplicationsPerYear,
    int CurrentRegistrationYear)
{
    public int PreviousRegistrationYear { get; } = CurrentRegistrationYear - 1;
};
