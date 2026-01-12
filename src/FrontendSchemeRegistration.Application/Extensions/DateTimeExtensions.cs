namespace FrontendSchemeRegistration.Application.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    ///     Get the Compliance Year for this <see cref="System.DateTime">DateTime</see>.
    ///     Generally, this will be the same as the actual year, but may differ within some special time windows.
    /// </summary>
    public static int GetComplianceYear(this DateTime date)
    {
        // Special case handling for Jan 2026
        if (date is { Year: 2026, Month: 1 }) return 2025;

        return date.Year;
    }
}