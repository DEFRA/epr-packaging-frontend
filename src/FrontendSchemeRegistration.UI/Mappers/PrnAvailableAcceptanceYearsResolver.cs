namespace FrontendSchemeRegistration.UI.Mappers;

using Application.DTOs.Prns;
using Application.Extensions;
using AutoMapper;

public class PrnAvailableAcceptanceYearsResolver(TimeProvider timeProvider)
    : IValueResolver<PrnModel, object, int[]>
{
    /// <summary>
    ///     Resolves the set of Years that a PRN may be accepted against at this specific point in time in the UI.
    /// </summary>
    /// <remarks>
    ///     Note that this is from a UI perspective - It does not return all the possible acceptance years for a PRN in
    ///     a static sense; this is explicitly returning what years (if any) are available for users action the PRN against
    ///     at this precise moment.
    ///     Generally, this will just resolve to a single year unless the PRN has expired - but December Waste PRNs may
    ///     have two.
    /// </remarks>
    public int[] Resolve(PrnModel source, object _, int[] __, ResolutionContext context)
    {
        if (!int.TryParse(source.ObligationYear, out var prnYear))
            return [];

        var now = timeProvider.GetUtcNow();
        var thisComplianceYear = now.GetComplianceYear();

        // Nonsense case, shouldn't happen. If it does - the current time would eventually catch up and the PRN would then be actionable.
        if (prnYear > thisComplianceYear)
            return [];

        // Special handling for December Waste
        if (source.DecemberWaste)
        {
            // Special handling for 2025 December Waste
            // Users do not get to choose between two years for these - effectively should just be the current Compliance Year
            if (prnYear == 2025 && thisComplianceYear is 2025 or 2026)
                return [thisComplianceYear];

            // Define the time window where users should have a choice of year for December Waste PRNs:
            // December of the PRN's year and January of the next (only).
            var windowStart = new DateTime(prnYear, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            var windowEnd = new DateTime(prnYear + 1, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            // See if we're within the time window where either year can be chosen by the user.
            if (now >= windowStart && now < windowEnd)
                return [prnYear, prnYear + 1];

            // Before the window, the PRN can only be accepted against its own year.
            if (prnYear == thisComplianceYear)
                return [prnYear];

            // Otherwise, December Waste should still be valid for the next Compliance Year (if current).
            if (prnYear + 1 == thisComplianceYear)
                return [thisComplianceYear];

            // Finally, December Waste has essentially expired at the current time.
            // It no longer has any available years to be accepted against.
            return [];
        }

        // The basic case for most PRNs
        if (prnYear == thisComplianceYear)
            return [prnYear];

        // PRN has essentially expired at the current time.
        // It no longer has any available years to be accepted against.
        return [];
    }
}