namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

public interface IRegistrationPeriodProvider
{
    IReadOnlyCollection<RegistrationWindow> GetRegistrationWindows(bool isCso);
}