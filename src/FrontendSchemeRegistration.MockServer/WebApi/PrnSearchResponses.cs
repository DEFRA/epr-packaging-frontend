namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock;
using WireMock.Types;
using WireMock.Util;

/// <summary>
/// Search stub for <c>/api/v1/prn/search</c>. When the caller sends <c>AcceptableIntoYear</c>, the
/// total and the items are the notes that can be accepted into that year. The obligations page
/// shows that total, so an unfiltered fixture total would disagree with the accept/reject list.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PrnSearchResponses
{
    private static readonly TimeZoneInfo UkZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static ResponseMessage For(IRequestMessage request, WebApiOptions options)
    {
        var json = Read(options.PrnSearchResponseFile);
        if (TryReadYear(request, out var year))
        {
            var document = JsonNode.Parse(json)!.AsObject();
            var matching = new JsonArray();
            foreach (var item in document["items"]?.AsArray() ?? [])
            {
                if (item is not null && IsAcceptableInto(item, year, options.UtcNow))
                {
                    matching.Add(item.DeepClone());
                }
            }

            document["items"] = matching;
            document["totalItems"] = matching.Count;
            json = document.ToJsonString();
        }

        return new ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>>
            {
                { "Content-Type", new WireMockList<string>("application/json") }
            },
            BodyData = new BodyData
            {
                BodyAsString = json,
                DetectedBodyType = BodyType.String
            }
        };
    }

    private static string Read(string fileName)
    {
        var besideAssembly = Path.Combine(
            Path.GetDirectoryName(typeof(PrnSearchResponses).Assembly.Location)!,
            "WebApi",
            "Responses",
            "WebApi",
            fileName);

        return File.ReadAllText(File.Exists(besideAssembly)
            ? besideAssembly
            : Path.Combine("WebApi", "Responses", "WebApi", fileName));
    }

    private static bool TryReadYear(IRequestMessage request, out int year)
    {
        year = 0;
        if (request.Query is null)
        {
            return false;
        }

        foreach (var pair in request.Query)
        {
            if (!pair.Key.Equals("AcceptableIntoYear", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = pair.Value?.FirstOrDefault();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out year);
        }

        return false;
    }

    private static bool IsAcceptableInto(JsonNode item, int year, DateTimeOffset now) =>
        TryReadObligationYear(item, out var obligationYear)
        && AvailableYears(obligationYear, ReadDecemberWaste(item), now).Contains(year);

    private static bool TryReadObligationYear(JsonNode item, out int year)
    {
        year = 0;
        if (item["obligationYear"] is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<int>(out year))
        {
            return true;
        }

        return value.TryGetValue<string>(out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out year);
    }

    private static bool ReadDecemberWaste(JsonNode item) =>
        item["decemberWaste"] is JsonValue value && value.TryGetValue<bool>(out var decemberWaste) && decemberWaste;

    private static int[] AvailableYears(int obligationYear, bool decemberWaste, DateTimeOffset now)
    {
        var thisComplianceYear = ComplianceYear(now);
        if (obligationYear > thisComplianceYear)
        {
            return [];
        }

        if (!decemberWaste)
        {
            return obligationYear == thisComplianceYear ? [obligationYear] : [];
        }

        if (obligationYear == 2025 && thisComplianceYear is 2025 or 2026)
        {
            return [thisComplianceYear];
        }

        var windowStart = new DateTimeOffset(obligationYear, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(obligationYear + 1, 2, 1, 0, 0, 0, TimeSpan.Zero);
        if (now >= windowStart && now < windowEnd)
        {
            return [obligationYear, obligationYear + 1];
        }

        if (obligationYear == thisComplianceYear)
        {
            return [obligationYear];
        }

        return obligationYear + 1 == thisComplianceYear ? [thisComplianceYear] : [];
    }

    private static int ComplianceYear(DateTimeOffset now)
    {
        var ukNow = TimeZoneInfo.ConvertTime(now, UkZone);

        return ukNow.Month == 1 ? ukNow.Year - 1 : ukNow.Year;
    }
}
