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
    /// Returns the parsed registration year if the input parameter is valid and matches the
    /// configured registration years and that registration year has started (based on whether
    /// the user's organisation is a CSO or not)
    /// </summary>
    /// <param name="registrationYear"></param>
    /// <param name="isParamOptional"></param>
    /// <returns></returns>
    int? ValidateRegistrationYear(string? registrationYear, bool isParamOptional = false);
}