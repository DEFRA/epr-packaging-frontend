namespace FrontendSchemeRegistration.UI.Mappers;

using Application.DTOs.Prns;
using Application.Extensions;
using AutoMapper;

public class PrnAvailableAcceptanceYearsResolver(TimeProvider timeProvider)
    : IValueResolver<PrnModel, object, HashSet<int>>
{
    /// <summary>
    ///     Resolves the set of Years that a PRN may be accepted against at the current time.
    /// </summary>
    /// <remarks>
    ///     Generally, this will just resolve to a single year unless the PRN has expired - but December Waste PRNs sometimes have two.
    /// </remarks>
    public HashSet<int> Resolve(PrnModel source, object _, HashSet<int> __, ResolutionContext context)
    {
        var now = timeProvider.GetUtcNow();
        var thisComplianceYear = now.GetComplianceYear();
        var prnObligationYear = int.TryParse(source.ObligationYear, out var year) ? year : 0;

        // Special handling for December Waste
        if (source.DecemberWaste)
        {
            // Special handling for 2025 December Waste
            if (prnObligationYear == 2025 && thisComplianceYear is 2025 or 2026)
                return [thisComplianceYear];

            // Define the time window where users should have a choice of year for December Waste PRNs.
            var windowEnd = new DateTime(prnObligationYear + 1, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            // See if we're within the time window where either year can be chosen by the user.
            if (now < windowEnd)
                return [prnObligationYear, prnObligationYear + 1];

            // Otherwise, December Waste should still be valid for the next Compliance Year.
            if (prnObligationYear + 1 == thisComplianceYear)
                return [thisComplianceYear];
        }

        // The basic case for most PRNs
        // Note that December waste may match this if it was issued prior to December for some reason (shouldn't happen).
        if (prnObligationYear == thisComplianceYear)
            return [prnObligationYear];

        // PRN has essentially expired at the current time. It no longer has any available years to be accepted against.
        return [];
    }
}