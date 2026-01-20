namespace FrontendSchemeRegistration.Application.Extensions;

public static class DateTimeExtensions
{
    // UK timezone (handles GMT/BST automatically)
    private static readonly TimeZoneInfo UkZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    /// <summary>
    ///     Get the Compliance Year for this <see cref="System.DateTime">DateTime</see>.
    ///     If the Kind is <see cref="DateTimeKind.Unspecified" />, it will be treated as UTC.
    /// </summary>
    /// <remarks>
    ///     The Compliance Year is based on the UK calendar year; the DateTime will be converted accordingly.
    ///     Note that the Compliance Year may differ from the actual UK calendar year for certain periods.
    /// </remarks>
    public static int GetComplianceYear(this DateTime dateTime)
    {
        var ukDatetime = dateTime.Kind switch
        {
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, UkZone),
            _ => TimeZoneInfo.ConvertTimeFromUtc(dateTime, UkZone)
        };

        return MapToComplianceYear(ukDatetime);
    }

    /// <summary>
    ///     Get the Compliance Year for this <see cref="System.DateTimeOffset">DateTimeOffset</see>.
    /// </summary>
    /// <remarks>
    ///     The Compliance Year is based on the UK calendar year; the DateTimeOffset will be converted accordingly.
    ///     Note that the Compliance Year may differ from the actual UK calendar year for certain periods.
    /// </remarks>
    public static int GetComplianceYear(this DateTimeOffset dateTimeOffset)
    {
        var ukDateTime = TimeZoneInfo.ConvertTime(dateTimeOffset, UkZone).DateTime;
        return MapToComplianceYear(ukDateTime);
    }

    private static int MapToComplianceYear(DateTime ukDateTime)
    {
        // Special case handling for Jan 2026
        if (ukDateTime is { Year: 2026, Month: 1 }) return 2025;

        return ukDateTime.Year;
    }
}