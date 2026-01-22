namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using Application.Enums;

/// <summary>
/// Represents a registration window
/// </summary>
public class RegistrationWindow
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _openingDate;
    private readonly DateTime _closingDate;

    /// <summary>
    /// Constructor used when we have a non-null registration journey
    /// </summary>
    /// <param name="timeProvider">Provides datetime operations</param>
    /// <param name="journey">The type of registration journey for this window</param>
    /// <param name="registrationYear">The registration year for which this window exists</param>
    /// <param name="openingDate">The window opens at 00:00 on this date. This is when registration opens</param>
    /// <param name="deadlineDate">This is the registration deadline after which late fees apply. The deadline closes at 00:00 on this date. Ie, late fees apply once this date is reached</param>
    /// <param name="closingDate">The window closes at 00:00 on this date. Ie, you can register up to, but not including, this date</param>
    public RegistrationWindow(TimeProvider timeProvider,
        RegistrationJourney journey,
        int registrationYear,
        DateTime openingDate,
        DateTime deadlineDate,
        DateTime closingDate
        )
    {
        _timeProvider = timeProvider;
        _openingDate = openingDate;
        _closingDate = closingDate;
        Journey = journey;
        RegistrationYear = registrationYear;
        DeadlineDate = deadlineDate;
        IsCso = IsRegistrationJourneyForCso(journey);
    }

    /// <summary>
    /// Constructor used when we have a null registration journey
    /// </summary>
    /// <param name="timeProvider">Provides datetime operations</param>
    /// <param name="isCso">Is the window that for a CSO</param>
    /// <param name="registrationYear">The registration year for which this window exists</param>
    /// <param name="openingDate">The window opens at 00:00 on this date. This is when registration opens</param>
    /// <param name="deadlineDate">This is the registration deadline after which late fees apply. The deadline closes at 00:00 on this date. Ie, late fees apply once this date is reached</param>
    /// <param name="closingDate">The window closes at 00:00 on this date. Ie, you can register up to, but not including, this date</param>
    public RegistrationWindow(TimeProvider timeProvider,
        bool isCso,
        int registrationYear,
        DateTime openingDate,
        DateTime deadlineDate,
        DateTime closingDate
    )
    {
        _timeProvider = timeProvider;
        _openingDate = openingDate;
        _closingDate = closingDate;
        Journey = null;
        RegistrationYear = registrationYear;
        DeadlineDate = deadlineDate;
        IsCso = isCso;
    }

    public RegistrationJourney? Journey { get; }
    public int RegistrationYear { get; }
    public DateTime DeadlineDate { get; }
    public bool IsCso { get; }

    public RegistrationWindowStatus GetRegistrationWindowStatus()
    {
        var now = _timeProvider.GetUtcNow(); 
        
        if (now < _openingDate)
        {
            return RegistrationWindowStatus.PriorToOpening;
        }
        else if (now >= _openingDate && now < DeadlineDate)
        {
            return RegistrationWindowStatus.OpenAndNotLate;
        }
        else if (now >= DeadlineDate && now < _closingDate)
        {
            return RegistrationWindowStatus.OpenAndLate;
        }
        else
        {
            return RegistrationWindowStatus.Closed;
        }
    }

    public static bool IsRegistrationJourneyForCso(RegistrationJourney journey) => 
        journey switch
        {
            RegistrationJourney.CsoLargeProducer or RegistrationJourney.CsoSmallProducer => true,
            RegistrationJourney.DirectLargeProducer or RegistrationJourney.DirectSmallProducer => false,
            _ => throw new ArgumentOutOfRangeException(nameof(journey), journey, null)
        };
}
