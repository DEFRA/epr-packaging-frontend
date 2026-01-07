namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using System.Collections.ObjectModel;
using Application.Enums;
using Application.Options.ReistrationPeriodPatterns;
using Microsoft.Extensions.Options;

internal class RegistrationPeriodProvider : IRegistrationPeriodProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly IEnumerable<RegistrationPeriodPattern> _registrationPeriodPatterns;
    private IReadOnlyCollection<RegistrationWindow> _registrationWindows = [];

    public RegistrationPeriodProvider(IOptions<List<RegistrationPeriodPattern>> registrationPeriodPatternOptions, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _registrationPeriodPatterns = registrationPeriodPatternOptions.Value;
        ParsePatterns();
    }

    private void ParsePatterns()
    {
        var windows = new List<RegistrationWindow>();
        foreach (var registrationPeriodPattern in _registrationPeriodPatterns)
        {
            // loop from initial registration year to final registration year (or this year, which ever is first), constructing windows
            var registrationYear = registrationPeriodPattern.InitialRegistrationYear;
            var finalYear = registrationPeriodPattern.FinalRegistrationYear ?? _timeProvider.GetUtcNow().Year;
            
            do
            {
                if (windows.Any(w => w.RegistrationYear == registrationYear)) throw new InvalidOperationException($"Registration year {registrationYear} is configured in multiple RegistrationPeriodPattern items within appsettings. The years between and including the InitialRegistrationYear and the FinalRegistrationYear may only exist in a single pattern.");
                
                foreach (var patternWindow in registrationPeriodPattern.Windows)
                {
                    var journey = MapWindowTypeToRegistrationJourney(patternWindow.WindowType);
                    
                    // don't bother adding windows whose closing date has passed. we will want to change this in future
                    // when we provide a panel for viewing historic submissions
                    var window = new RegistrationWindow(_timeProvider, journey, registrationYear,
                            new DateTime(registrationYear + patternWindow.OpeningDate.YearOffset,
                                patternWindow.OpeningDate.Month, patternWindow.OpeningDate.Day),
                            new DateTime(registrationYear + patternWindow.DeadlineDate.YearOffset,
                                patternWindow.DeadlineDate.Month, patternWindow.DeadlineDate.Day),
                            new DateTime(registrationYear + patternWindow.ClosingDate.YearOffset,
                                patternWindow.ClosingDate.Month, patternWindow.ClosingDate.Day));

                    if (window.GetRegistrationWindowStatus() != RegistrationWindowStatus.Closed)
                    {
                        windows.Add(window);
                    }
                }

                registrationYear++;
            } while (registrationYear <= finalYear);
        }
        _registrationWindows = windows.OrderByDescending(w => w.RegistrationYear).ToList();
    }

    public IReadOnlyCollection<RegistrationWindow> GetRegistrationWindows(bool isCso)
    {
        var windows = _registrationWindows.Where(w =>
        {
            if (isCso)
            {
                return w.Journey == RegistrationJourney.CsoLargeProducer ||
                       w.Journey == RegistrationJourney.CsoSmallProducer || w.Journey is null;
            }
            else
            {
                return w.Journey == RegistrationJourney.DirectLargeProducer ||
                       w.Journey == RegistrationJourney.DirectSmallProducer || w.Journey is null;
            }
        });
        
        return new ReadOnlyCollection<RegistrationWindow>(windows.ToList());
    }

    private static RegistrationJourney? MapWindowTypeToRegistrationJourney(WindowType windowType) =>
        windowType switch
        {
            WindowType.CsoLargeProducer => RegistrationJourney.CsoLargeProducer,
            WindowType.CsoSmallProducer => RegistrationJourney.CsoSmallProducer,
            WindowType.DirectLargeProducer => RegistrationJourney.DirectLargeProducer,
            WindowType.DirectSmallProducer => RegistrationJourney.DirectSmallProducer,
            _ => null
        };
}