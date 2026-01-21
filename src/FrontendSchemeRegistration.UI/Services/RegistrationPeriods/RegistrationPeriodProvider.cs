namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Application.Enums;
using Application.Options.ReistrationPeriodPatterns;
using Microsoft.Extensions.Options;

/// <summary>
/// This provides access to registration window data. It is registered as a singleton, however, the windows that it
/// returns are not registered in the DI container because available windows are date dependent.
/// </summary>
internal class RegistrationPeriodProvider : IRegistrationPeriodProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly IEnumerable<RegistrationPeriodPattern> _registrationPeriodPatterns;
    private IReadOnlyCollection<RegistrationWindow> _registrationWindows = [];
    private int? _parseYear;    // the year when Parse was last called
    
    private readonly object _lock = new();

    public RegistrationPeriodProvider(IOptions<List<RegistrationPeriodPattern>> registrationPeriodPatternOptions, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _registrationPeriodPatterns = registrationPeriodPatternOptions.Value;
        ParsePatterns();
    }

    /// <summary>
    /// Rebuilds the window collection 
    /// </summary>
    private void ParsePatterns()
    {
        _parseYear = _timeProvider.GetUtcNow().Year;
        var windows = new List<RegistrationWindow>();
        
        foreach (var registrationPeriodPattern in _registrationPeriodPatterns)
        {
            // loop from initial registration year to final registration year (or this year, which ever is first), constructing windows
            var registrationYear = registrationPeriodPattern.InitialRegistrationYear;
            var finalYear = ParseFinalYear(registrationPeriodPattern);
            
            // loop through the registration years in this pattern
            do
            {
                // this is checking BEFORE going through the windows in the new pattern, so 
                EnsureRegistrationYearNotDuplicated(windows, registrationYear);

                foreach (var patternWindow in registrationPeriodPattern.Windows)
                {
                    var journey = MapWindowTypeToRegistrationJourney(patternWindow.WindowType);
                    var closeDate = new DateTime(registrationYear + patternWindow.ClosingDate.YearOffset,
                        patternWindow.ClosingDate.Month, patternWindow.ClosingDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    var openingDate = new DateTime(registrationYear + patternWindow.OpeningDate.YearOffset,
                        patternWindow.OpeningDate.Month, patternWindow.OpeningDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    var deadlineDate = new DateTime(registrationYear + patternWindow.DeadlineDate.YearOffset,
                        patternWindow.DeadlineDate.Month, patternWindow.DeadlineDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    
                    if (journey.RegistrationJourney is null)
                    {
                        windows.Add(new RegistrationWindow(_timeProvider, journey.IsCso, registrationYear, openingDate, deadlineDate, closeDate));    
                    }
                    else
                    {
                        windows.Add(new RegistrationWindow(_timeProvider, journey.RegistrationJourney.Value, registrationYear, openingDate, deadlineDate, closeDate));
                    }
                }

                registrationYear++;
            } while (registrationYear <= finalYear);
        }
        
        _registrationWindows = [..windows.OrderByDescending(w => w.RegistrationYear)];
    }

    /// <summary>
    /// Gets active registration windows. These include closed windows, as producers may still register
    /// beyond the closing date
    /// </summary>
    /// <param name="isCso"></param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public IReadOnlyCollection<RegistrationWindow> GetActiveRegistrationWindows(bool isCso)
    {
        // Rebuild the collection if we tick into the next year and we have
        // derived the final year in a pattern (ie, the final year in config is null)
        if (_timeProvider.GetUtcNow().Year > _parseYear)
        {
            lock (_lock)
            {
                if (_timeProvider.GetUtcNow().Year > _parseYear)
                {
                    ParsePatterns();
                }
            }
        }
        
        var orderedWindows = _registrationWindows
            .Where(w => w.IsCso == isCso && w.GetRegistrationWindowStatus() != RegistrationWindowStatus.PriorToOpening)
            .OrderByDescending(ra => ra.RegistrationYear);

        return new ReadOnlyCollection<RegistrationWindow>(orderedWindows.ToList());
    }

    /// <summary>
    /// Gets the final year for a registration period pattern, validates and stores the derived value.
    /// If the pattern does not explicitly include a final year, then we derive a value for which we
    /// generate data, based on the opening date and the current year (ie, if the opening date has a year
    /// offset of -1, then we need to generate registration window data for next year's registration) 
    /// </summary>
    /// <param name="registrationPeriodPattern"></param>
    /// <returns>The final year for a registration period</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private int ParseFinalYear(RegistrationPeriodPattern registrationPeriodPattern)
    {
        int finalYear;
        if (registrationPeriodPattern.FinalRegistrationYear.HasValue)
        {
            finalYear = registrationPeriodPattern.FinalRegistrationYear.Value;
        }
        else
        {
            // we might want to create entries for a future registration year, in case the start date is the year before
            // get the largest year offset for all of the windows` start dates
            var earliestOpeningDateYearOffset = registrationPeriodPattern.Windows.Min(w => w.OpeningDate.YearOffset);

            // eg, if the opening date offset is -1, and this year is 2026, then we need to generate data for 2027 = 2026 - (-1)
            finalYear = _timeProvider.GetUtcNow().Year - earliestOpeningDateYearOffset;
        }

        return finalYear;
    }

    /// <summary>
    /// Ensures that the registration year does not already exist in the list of windows
    /// </summary>
    /// <param name="windows"></param>
    /// <param name="registrationYear"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void EnsureRegistrationYearNotDuplicated(List<RegistrationWindow> windows, int registrationYear)
    {
        if (windows.Any(w => w.RegistrationYear == registrationYear)) throw new InvalidOperationException($"Registration year {registrationYear} is configured in multiple RegistrationPeriodPattern items within appsettings. The years between and including the InitialRegistrationYear and the FinalRegistrationYear may only exist in a single pattern.");
    }

    private static (RegistrationJourney? RegistrationJourney, bool IsCso) MapWindowTypeToRegistrationJourney(WindowType windowType) =>
        windowType switch
        {
            WindowType.CsoLargeProducer => (RegistrationJourney.CsoLargeProducer, RegistrationWindow.IsRegistrationJourneyForCso(RegistrationJourney.CsoLargeProducer)),
            WindowType.CsoSmallProducer => (RegistrationJourney.CsoSmallProducer, RegistrationWindow.IsRegistrationJourneyForCso(RegistrationJourney.CsoSmallProducer)),
            WindowType.DirectLargeProducer => (RegistrationJourney.DirectLargeProducer, RegistrationWindow.IsRegistrationJourneyForCso(RegistrationJourney.DirectLargeProducer)),
            WindowType.DirectSmallProducer => (RegistrationJourney.DirectSmallProducer, RegistrationWindow.IsRegistrationJourneyForCso(RegistrationJourney.DirectSmallProducer)),
            WindowType.Cso => (null, true),
            _ => (null, false)
        };
}