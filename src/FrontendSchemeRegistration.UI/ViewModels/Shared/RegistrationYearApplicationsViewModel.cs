namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

public record class RegistrationYearApplicationsViewModel(int Year, IEnumerable<RegistrationApplicationViewModel> Applications);