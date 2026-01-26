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
    ///     Note that the Compliance Year will differ from the actual UK calendar year for certain periods -
    ///     it runs from Feb-Jan rather than Jan-Dec
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
    ///     Note that the Compliance Year will differ from the actual UK calendar year for certain periods -
    ///     it runs from Feb-Jan rather than Jan-Dec
    /// </remarks>
    public static int GetComplianceYear(this DateTimeOffset dateTimeOffset)
    {
        var ukDateTime = TimeZoneInfo.ConvertTime(dateTimeOffset, UkZone).DateTime;
        return MapToComplianceYear(ukDateTime);
    }

    private static int MapToComplianceYear(DateTime ukDateTime)
    {
        if (ukDateTime is { Month: 1 }) return ukDateTime.Year - 1;

        return ukDateTime.Year;
    }
}