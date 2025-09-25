using System.Globalization;

namespace FrontendSchemeRegistration.Application.Options;

public class LargeProducerRegistrationWarning
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }

    public bool IsActiveToday()
    {
        if (DateTime.TryParseExact(StartDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateTime.TryParseExact(EndDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            var today = DateTime.UtcNow.Date;
            return today >= start && today <= end;
        }

        return false;
    }
}

