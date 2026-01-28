namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using Application.Options.RegistrationPeriodPatterns;

/// <summary>
/// Represents a registration window
/// </summary>
public class RegistrationWindow
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _closingDate;

    /// <summary>
    /// Constructs a registration window
    /// </summary>
    /// <param name="timeProvider">Provides datetime operations</param>
    /// <param name="windowType">The type of window</param>
    /// <param name="registrationYear">The registration year for which this window exists</param>
    /// <param name="openingDate">The window opens at 00:00 on this date. This is when registration opens</param>
    /// <param name="deadlineDate">This is the registration deadline after which late fees apply. The deadline closes at 00:00 on this date. Ie, late fees apply once this date is reached</param>
    /// <param name="closingDate">The window closes at 00:00 on this date. Ie, you can register up to, but not including, this date</param>
    public RegistrationWindow(TimeProvider timeProvider,
        WindowType windowType,
        int registrationYear,
        DateTime openingDate,
        DateTime deadlineDate,
        DateTime closingDate
        )
    {
        _timeProvider = timeProvider;
        _closingDate = closingDate;
        WindowType = windowType;
        RegistrationYear = registrationYear;
        OpeningDate = openingDate;
        DeadlineDate = deadlineDate;
        IsCso = IsRegistrationWindowForCso(windowType);
    }

    public WindowType WindowType { get; }
    public int RegistrationYear { get; }
    public DateTime OpeningDate { get; }
    public DateTime DeadlineDate { get; }
    public bool IsCso { get; }

    public RegistrationWindowStatus GetRegistrationWindowStatus()
    {
        var now = _timeProvider.GetUtcNow(); 
        
        if (now < OpeningDate)
        {
            return RegistrationWindowStatus.PriorToOpening;
        }
        else if (now >= OpeningDate && now < DeadlineDate)
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

    private static bool IsRegistrationWindowForCso(WindowType windowType) => 
        windowType switch
        {
            WindowType.Cso or WindowType.CsoLargeProducer or WindowType.CsoSmallProducer => true,
            _ => false
        };
}
