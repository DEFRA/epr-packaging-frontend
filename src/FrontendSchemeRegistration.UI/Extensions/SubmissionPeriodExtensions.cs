using System.Globalization;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using Microsoft.IdentityModel.Tokens;

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

        if (month.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return DateTime.Parse($"1 {month} {period.Year}")
            .ToString("MMMM")
            .Replace("Mehefin", "Fehefin")
            .Replace("Rhagfyr", "Ragfyr");
    }

    public static string LocalisedMonth(this SubmissionPeriodId period, Enums.MonthType? monthType)
    {
        if (period is null || period.SubmissionPeriod.IsNullOrEmpty())
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