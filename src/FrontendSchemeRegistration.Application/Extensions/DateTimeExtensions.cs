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
        return MapToComplianceYear(ToUkDateTime(dateTime));
    }

    /// <summary>
    ///     Whether the UK calendar date of <paramref name="dateTime"/> is after the calendar date of <paramref name="date"/>.
    ///     If the Kind of <paramref name="dateTime"/> is <see cref="DateTimeKind.Unspecified" />, it will be treated as UTC.
    /// </summary>
    /// <remarks>
    ///     <paramref name="date"/> is already a UK calendar date (e.g. the CSoC submission deadline), so only its date part is used.
    /// </remarks>
    public static bool IsAfterUkDate(this DateTime dateTime, DateTime date)
    {
        return ToUkDateTime(dateTime).Date > date.Date;
    }

    private static DateTime ToUkDateTime(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, UkZone),
            _ => TimeZoneInfo.ConvertTimeFromUtc(dateTime, UkZone)
        };
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

    /// <summary>
    ///     Whether <paramref name="now"/> falls in the UK December/January flash window that starts in
    ///     December of <paramref name="accreditationYear"/>: from 1 Dec Y through end of Jan Y+1.
    /// </summary>
    public static bool IsInDecemberJanuaryFlashWindow(
        this DateTimeOffset now,
        int accreditationYear)
    {
        var ukNow = TimeZoneInfo.ConvertTime(now, UkZone).DateTime;
        var windowStart = new DateTime(accreditationYear, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var windowEnd = new DateTime(accreditationYear + 1, 2, 1, 0, 0, 0, DateTimeKind.Unspecified);

        return ukNow >= windowStart && ukNow < windowEnd;
    }

    /// <summary>
    ///     Whether <paramref name="now"/> falls within the UK December/January flash window,
    ///     i.e. whether the UK calendar month is December or January.
    /// </summary>
    /// <remarks>
    ///     Use this when there is no specific PRN accreditation year to check against (e.g. deciding
    ///     whether to show flash-related UI at the page level). To check a specific PRN's
    ///     accreditation year, use <see cref="IsInDecemberJanuaryFlashWindow(DateTimeOffset, int)"/> instead.
    /// </remarks>
    public static bool IsInDecemberJanuaryFlashWindow(this DateTimeOffset now)
    {
        var ukNow = TimeZoneInfo.ConvertTime(now, UkZone).DateTime;
        return ukNow.Month is 1 or 12;
    }

    /// <summary>
    ///     The next CSoC submission deadline: 31 January. Submissions are accepted until the end of that day,
    ///     so the deadline only moves on to the following year from 1 February.
    /// </summary>
    public static DateTime GetCsocSubmissionDeadline(this DateTime now)
    {
        var year = now.Date > new DateTime(now.Year, 1, 31, 0, 0, 0, DateTimeKind.Unspecified)
            ? now.Year + 1
            : now.Year;

        return new DateTime(year, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
    }
}