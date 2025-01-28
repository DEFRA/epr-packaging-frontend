using System.Globalization;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.Extensions;

public static class SubmissionPeriodExtensions
{
    public static string LocalisedMonth(this SubmissionPeriod period, Enums.MonthType? monthType)
    {
        if (!monthType.HasValue)
        {
            return string.Empty;
        }

        var month = monthType switch
        {
            Enums.MonthType.Start => period.StartMonth,
            Enums.MonthType.End => period.EndMonth,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(month))
        {
            return string.Empty;
        }

        return DateTime.Parse($"1 {month} {period.Year}", new CultureInfo("en-GB"))
            .ToString("MMMM")
            .Replace("Mehefin", "Fehefin")
            .Replace("Rhagfyr", "Ragfyr");
    }

    public static string LocalisedShortMonth(this SubmissionPeriod period, Enums.MonthType? monthType)
    {
        if (!monthType.HasValue)
        {
            return string.Empty;
        }
        var month = monthType switch
        {
            Enums.MonthType.Start => period.StartMonth,
            Enums.MonthType.End => period.EndMonth,
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(month))
        {
            return string.Empty;
        }
        return DateTime.Parse($"1 {month} {period.Year}", new CultureInfo("en-GB"))
            .ToString("MMM");
    }

    public static string LocalisedMonth(this SubmissionPeriodId period, Enums.MonthType? monthType)
    {
        if (string.IsNullOrWhiteSpace(period.SubmissionPeriod))
        {
            return string.Empty;
        }

        var dateBreak = period.SubmissionPeriod.ToStartEndDate();
        var submissionPeriod = new SubmissionPeriod
        {
            StartMonth = dateBreak.Start.ToString("MMMM", CultureInfo.InvariantCulture),
            EndMonth = dateBreak.End.ToString("MMMM", CultureInfo.InvariantCulture),
            Year = dateBreak.Start.Year.ToString()
        };
        return submissionPeriod.LocalisedMonth(monthType);
    }
}