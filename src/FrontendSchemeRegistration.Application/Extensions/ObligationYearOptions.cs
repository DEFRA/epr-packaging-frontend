namespace FrontendSchemeRegistration.Application.Extensions;

public static class ObligationYearOptions
{
    public const int EarliestHistoricalYear = 2025;

    /// <summary>
    ///     Years shown on the Choose a year page for <paramref name="currentComplianceYear"/>,
    ///     newest first: future year, current year, then historical years back to
    ///     <see cref="EarliestHistoricalYear"/>.
    /// </summary>
    public static IReadOnlyList<int> GetSelectableYears(int currentComplianceYear)
    {
        var years = new List<int> { currentComplianceYear + 1, currentComplianceYear };

        for (var year = currentComplianceYear - 1; year >= EarliestHistoricalYear; year--)
        {
            years.Add(year);
        }

        return years;
    }
}
