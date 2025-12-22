namespace FrontendSchemeRegistration.UI.Extensions;

public static class DateTimeExtensions
{
    public static string ToReadableDate(this DateTime dateTime) => dateTime.UtcToGmt().ToString("d MMMM yyyy");

    public static string ToReadableDateTime(this DateTime dateTime) => dateTime.UtcToGmt().ToString("d MMMM yyyy, hh:mm") + dateTime.UtcToGmt().ToString("tt").ToLower();

    public static string ToReadableLongMonthDeadlineDate(this DateTime dateTime) => dateTime.ToString("d MMMM yyyy");

    public static string ToShortReadableDate(this DateTime dateTime) => dateTime.UtcToGmt().ToString("d MMM yyyy");

    public static string ToShortReadableWithShortYearDate(this DateTime dateTime) => dateTime.UtcToGmt().ToString("d MMM yy");

    public static string ToTimeHoursMinutes(this DateTime dateTime) => dateTime.UtcToGmt().ToString("h:mmtt").ToLower();

    public static DateTime UtcToGmt(this DateTime dateTime) => TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));
}
