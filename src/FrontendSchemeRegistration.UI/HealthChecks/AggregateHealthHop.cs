namespace FrontendSchemeRegistration.UI.HealthChecks;

using System.Globalization;

public static class AggregateHealthHop
{
    public const string HeaderName = "X-EPR-Health-Check-Hop";

    public static bool TryRead(HttpRequest request, int maximumHops, out int hop)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var values))
        {
            hop = 0;
            return true;
        }

        if (values.Count != 1
            || !int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out hop)
            || hop > maximumHops)
        {
            hop = 0;
            return false;
        }

        return true;
    }

    public static void AddTo(HttpRequestMessage request, int hop) =>
        request.Headers.TryAddWithoutValidation(HeaderName, (hop + 1).ToString(CultureInfo.InvariantCulture));
}
