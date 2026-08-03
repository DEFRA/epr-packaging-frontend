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

    /// <summary>
    ///     Whether <paramref name="issueDate"/> falls in the immediate UK December/January flash window
    ///     for <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    ///     Flash only applies while <paramref name="now"/> is in December or January.
    ///     The immediate window is:
    ///     <list type="bullet">
    ///         <item>In December Y: from 1 Dec Y through end of Jan Y+1</item>
    ///         <item>In January Y: from 1 Dec Y-1 through end of Jan Y</item>
    ///     </list>
    ///     So a January issue from the previous window (e.g. Jan 2025 when now is Jan 2026) is excluded.
    /// </remarks>
    public static bool IsInImmediateDecemberJanuaryFlashWindow(
        this DateTimeOffset now,
        DateTime issueDate)
    {
        var ukNow = TimeZoneInfo.ConvertTime(now, UkZone).DateTime;
        if (ukNow.Month is not (1 or 12))
            return false;

        var ukIssue = issueDate.Kind switch
        {
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(issueDate, UkZone),
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(issueDate, UkZone),
            _ => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(issueDate, DateTimeKind.Utc), UkZone)
        };

        var windowStartYear = ukNow.Month == 12 ? ukNow.Year : ukNow.Year - 1;
        var windowStart = new DateTime(windowStartYear, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var windowEnd = new DateTime(windowStartYear + 1, 2, 1, 0, 0, 0, DateTimeKind.Unspecified);

        return ukIssue >= windowStart && ukIssue < windowEnd;
    }

    public static DateTime GetCsocSubmissionDeadline(this DateTime now)
    {
        var year = now > new DateTime(now.Year, 1, 31, 0, 0, 0, DateTimeKind.Unspecified)
            ? now.Year + 1
            : now.Year;

        return new DateTime(year, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
    }
}