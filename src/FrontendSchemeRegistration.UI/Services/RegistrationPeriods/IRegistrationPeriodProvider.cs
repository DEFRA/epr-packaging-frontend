namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

public interface IRegistrationPeriodProvider
{
    /// <summary>
    /// Returns registration windows that are open. Orders them by descending registration year
    /// </summary>
    /// <param name="isCso">Should the windows be those for CSOs</param>
    /// <returns>All open registration windows</returns>
    IReadOnlyCollection<RegistrationWindow> GetActiveRegistrationWindows(bool isCso);

    /// <summary>
    /// Returns all registration windows, whether they are closed or in the future. Will return
    /// a future window if that window's opening date is this year
    /// </summary>
    /// <param name="isCso"></param>
    /// <returns>All past, current or future registration windows</returns>
    IReadOnlyCollection<RegistrationWindow> GetAllRegistrationWindows(bool isCso);
}