namespace FrontendSchemeRegistration.UI.Services;

using System.Globalization;
using Application.DTOs.Submission;

public static class ReferenceNumberBuilder
{
    public static string Build(
        SubmissionPeriod period,
        string organisationNumber,
        TimeProvider tp,
        bool isComplianceScheme,
        int? complianceSchemeRowNumber,
        string? registrationJourney)
    {
        string regSize;
        if (string.IsNullOrWhiteSpace(registrationJourney))
        {
            regSize = string.Empty;
        }
        else
        {
            regSize = registrationJourney.ToLower().Contains("large") ? "L" : "S";
        }
        
        var referenceNumber = organisationNumber;
        var intMonth = DateTime.Parse("20 " + period.EndMonth + " 2000", CultureInfo.InvariantCulture).Month;
        var daysInMonth = DateTime.DaysInMonth(Convert.ToInt32(period.Year), intMonth);
        var periodEnd = DateTime.Parse($"{daysInMonth} {period.EndMonth} {period.Year}", new CultureInfo("en-GB"));
        
        var today = tp.GetLocalNow().Date;
        var periodNumber = today <= periodEnd ? 1 : 2;

        if (isComplianceScheme)
        {
            referenceNumber += complianceSchemeRowNumber?.ToString("D3");
        }

        return $"PEPR{referenceNumber}{periodEnd.Year - 2000}P{periodNumber}{regSize}";
    }
}