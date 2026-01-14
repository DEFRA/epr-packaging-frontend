namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

public interface IRegistrationPeriodProvider
{
    /// <summary>
    /// Returns registration windows that are open. Orders them by descending registration year
    /// </summary>
    /// <param name="isCso">Should the windows be those for CSOs</param>
    /// <returns>All open registration windows</returns>
    IReadOnlyCollection<RegistrationWindow> GetActiveRegistrationWindows(bool isCso);
}