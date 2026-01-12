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
    private int? _derivedFinalYear;
    
    // store the next upcoming close date, as we can trigger a rebuild after we pass it
    private DateTime _nextCloseDate;
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
        _derivedFinalYear = null;
        _nextCloseDate = DateTime.MaxValue;
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
                    
                    // don't bother adding windows whose closing date has passed. we will want to change this in future
                    // when we provide a panel for viewing historic submissions
                    var window = new RegistrationWindow(_timeProvider, journey, registrationYear,
                            new DateTime(registrationYear + patternWindow.OpeningDate.YearOffset,
                                patternWindow.OpeningDate.Month, patternWindow.OpeningDate.Day),
                            new DateTime(registrationYear + patternWindow.DeadlineDate.YearOffset,
                                patternWindow.DeadlineDate.Month, patternWindow.DeadlineDate.Day),
                            closeDate);

                    if (window.GetRegistrationWindowStatus() != RegistrationWindowStatus.Closed)
                    {
                        windows.Add(window);

                        UpdateNextCloseDate(closeDate);
                    }
                }

                registrationYear++;
            } while (registrationYear <= finalYear);
        }
        
        _registrationWindows = [..windows.OrderByDescending(w => w.RegistrationYear)];
    }

    /// <summary>
    /// Gets registration windows.
    /// </summary>
    /// <param name="isCso"></param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public IReadOnlyCollection<RegistrationWindow> GetRegistrationWindows(bool isCso)
    {
        // Rebuild the collection if we tick into the next year and we have
        // derived the final year in a pattern (ie, the final year in config is null)
        if (_timeProvider.GetUtcNow().Year > _derivedFinalYear)
        {
            lock (_lock)
            {
                if (_timeProvider.GetUtcNow().Year > _derivedFinalYear)
                {
                    ParsePatterns();
                }
            }
        }
        
        // Rebuild the collection if we pass the next stored close date, as we don't want
        // to return closed windows
        if (_timeProvider.GetUtcNow() > _nextCloseDate)
        {
            lock (_lock)
            {
                if (_timeProvider.GetUtcNow() > _nextCloseDate)
                {
                    ParsePatterns();
                }
            }
        }
        
        var windows = _registrationWindows.Where(w =>
        {
            if (isCso)
            {
                return w.Journey is RegistrationJourney.CsoLargeProducer or RegistrationJourney.CsoSmallProducer or null;
            }
            else
            {
                return w.Journey is RegistrationJourney.DirectLargeProducer or RegistrationJourney.DirectSmallProducer or null;
            }
        });
        
        return new ReadOnlyCollection<RegistrationWindow>(windows.ToList());
    }

    /// <summary>
    /// Gets the final year for a registration period pattern, validates and stores the derived value
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
            if (_derivedFinalYear.HasValue) throw new InvalidOperationException("You may not have multiple RegistrationPeriodPattern configuration items with a null FinalRegistrationYear value");
                
            // we store this so that we know if the year rolls over when GetRegistrationWindows is called
            _derivedFinalYear = _timeProvider.GetUtcNow().Year;
            finalYear = _derivedFinalYear.Value;
        }

        return finalYear;
    }

    /// <summary>
    /// Stores the next close date
    /// </summary>
    /// <param name="closeDate"></param>
    private void UpdateNextCloseDate(DateTime closeDate)
    {
        if (closeDate < _nextCloseDate)
        {
            _nextCloseDate = closeDate;
        }
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