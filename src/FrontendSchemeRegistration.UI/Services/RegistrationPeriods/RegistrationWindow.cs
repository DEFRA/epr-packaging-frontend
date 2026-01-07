namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using Application.Enums;

/// <summary>
/// Represents a registration window
/// </summary>
/// <param name="timeProvider">Provides datetime operations</param>
/// <param name="journey">The type of registration journey for this window</param>
/// <param name="registrationYear">The registration year for which this window exists</param>
/// <param name="openingDate">The window opens at 00:00 on this date. This is when registration opens</param>
/// <param name="deadlineDate">This is the registration deadline after which late fees apply. The deadline closes at 00:00 on this date. Ie, late fees apply once this date is reached</param>
/// <param name="closingDate">The window closes at 00:00 on this date. Ie, you can register up to, but not including, this date</param>
public class RegistrationWindow(
    TimeProvider timeProvider,
    RegistrationJourney? journey,
    int registrationYear,
    DateTime openingDate,
    DateTime deadlineDate,
    DateTime closingDate)
{
    public RegistrationJourney? Journey { get; } = journey;
    public int RegistrationYear { get; } = registrationYear;
    public DateTime DeadlineDate { get; } = deadlineDate;

    public RegistrationWindowStatus GetRegistrationWindowStatus()
    {
        var now = timeProvider.GetUtcNow(); 
        
        if (now < openingDate)
        {
            return RegistrationWindowStatus.PriorToOpening;
        }
        else if (now > openingDate && now < DeadlineDate)
        {
            return RegistrationWindowStatus.OpenAndNotLate;
        }
        else if (now > DeadlineDate && now < closingDate)
        {
            return RegistrationWindowStatus.OpenAndLate;
        }
        else
        {
            return RegistrationWindowStatus.Closed;
        }
    }
}
