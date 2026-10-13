namespace FrontendSchemeRegistration.Application.Extensions;

public static class ObligationYearOptions
{
    public const int EarliestHistoricalYear = 2025;

    /// <summary>
    ///     Years shown on the Choose a year page for <paramref name="currentComplianceYear"/>,
    ///     newest first: future year (when <paramref name="includeFutureYear"/>), current year,
    ///     then historical years back to <see cref="EarliestHistoricalYear"/>.
    /// </summary>
    public static IReadOnlyList<int> GetSelectableYears(int currentComplianceYear, bool includeFutureYear = true)
    {
        var years = new List<int>();

        if (includeFutureYear)
        {
            years.Add(currentComplianceYear + 1);
        }

        years.Add(currentComplianceYear);

        for (var year = currentComplianceYear - 1; year >= EarliestHistoricalYear; year--)
        {
            years.Add(year);
        }

        return years;
    }
}
