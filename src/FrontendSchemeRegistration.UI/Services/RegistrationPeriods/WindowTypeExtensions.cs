namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using Application.Options.RegistrationPeriodPatterns;
using FrontendSchemeRegistration.Application.Enums;

public static class WindowTypeExtensions
{
    /// <summary>
    /// Converts a window type to a registration journey used in the UI. Legacy windows
    /// use a null registration journey.
    /// </summary>
    /// <param name="windowType"></param>
    /// <returns></returns>
    public static RegistrationJourney? ToRegistrationJourney(this WindowType windowType) =>
        windowType switch
        {
            WindowType.CsoLargeProducer => RegistrationJourney.CsoLargeProducer,
            WindowType.CsoSmallProducer => RegistrationJourney.CsoSmallProducer,
            WindowType.DirectLargeProducer => RegistrationJourney.DirectLargeProducer,
            WindowType.DirectSmallProducer => RegistrationJourney.DirectSmallProducer,
            _ => null
        };
}