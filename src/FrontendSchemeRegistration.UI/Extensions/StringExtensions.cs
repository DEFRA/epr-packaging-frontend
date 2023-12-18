namespace FrontendSchemeRegistration.UI.Extensions;

using System.Text.RegularExpressions;

public static class StringExtensions
{
    /// <summary>Extracts start and end date from a string.</summary>
    /// <remarks>Period string should be in 'Jan to Jun 2023' format.</remarks>
    /// <param name="periodString">The period string.</param>
    /// <returns>Start and end dates.</returns>
    public static (DateTime Start, DateTime End) ToStartEndDate(this string periodString)
    {
        var timeout = TimeSpan.FromSeconds(1);
        string yearString = Regex.Match(periodString, @"\d+", RegexOptions.None, timeout).Value;
        string monthPeriod = periodString[..^yearString.Length];
        string[] startEndMonths = monthPeriod.Split("to", StringSplitOptions.TrimEntries);
        string start = $"1 {startEndMonths.FirstOrDefault()} {yearString}";
        string end = $"1 {startEndMonths.LastOrDefault()} {yearString}";

        if (DateTime.TryParse(start, out var startDate) && DateTime.TryParse(end, out var endDate))
        {
            return (Start: startDate, End: new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month)));
        }

        return (Start: DateTime.MinValue, End: DateTime.MinValue);
    }

    public static string ToReferenceNumberFormat(this string? str)
    {
        if(str is null)
        {
            return string.Empty;
        }

        str = str.Replace(" ", string.Empty);

        int i = str.Length - 3;
        while (i > 0)
        {
            str = str.Insert(i, " ");
            i -= 3;
        }

        return str;
    }
}
