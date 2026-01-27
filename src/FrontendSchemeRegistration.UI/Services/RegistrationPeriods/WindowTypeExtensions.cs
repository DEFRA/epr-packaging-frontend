namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using Application.Options.RegistrationPeriodPatterns;
using FrontendSchemeRegistration.Application.Enums;

public static class WindowTypeExtensions
{
    /// <summary>
    /// Converts a window type to a registration journey used in the UI. Direct
    /// registration uses a null registration journey because the UI has no way of
    /// knowing which type of registration is being performed - large or small - given
    /// that the user is given a single tile and doesn't specify whether they are
    /// large or small. That gets determined from the CSV file later on.
    /// </summary>
    /// <param name="windowType"></param>
    /// <returns></returns>
    public static RegistrationJourney? ToRegistrationJourney(this WindowType windowType) =>
        windowType switch
        {
            WindowType.CsoLargeProducer => RegistrationJourney.CsoLargeProducer,
            WindowType.CsoSmallProducer => RegistrationJourney.CsoSmallProducer,
            _ => null
        };
}