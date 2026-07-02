using System.Text.RegularExpressions;

namespace Translations.Services;

internal static partial class TranslationValueValidator
{
    public static IReadOnlyList<string> Validate(string translationKey, string english, string welsh)
    {
        var errors = new List<string>();

        if (!string.Equals(welsh.Trim(), welsh, StringComparison.Ordinal))
        {
            errors.Add($"{translationKey}: translated values must not include leading or trailing whitespace. Move spacing into the layout instead.");
        }

        if (EncodedHtmlTagRegex().IsMatch(welsh))
        {
            errors.Add($"{translationKey}: contains encoded HTML such as &lt;strong&gt;. Use decoded tags such as <strong>; import will encode them for RESX.");
        }

        AddTokenErrors(
            errors,
            translationKey,
            "placeholder",
            "Preserve placeholders exactly, including format suffixes such as {0:d MMMM yyyy}.",
            CountMatches(english, PlaceholderRegex()),
            CountMatches(welsh, PlaceholderRegex()));

        AddTokenErrors(
            errors,
            translationKey,
            "markup tag",
            "Preserve markup tags exactly as tags, not XML entities.",
            CountMatches(english, HtmlTagRegex(), match => NormalizeHtmlTag(match.Value)),
            CountMatches(welsh, HtmlTagRegex(), match => NormalizeHtmlTag(match.Value)));

        return errors;
    }

    private static void AddTokenErrors(
        List<string> errors,
        string translationKey,
        string tokenType,
        string guidance,
        IReadOnlyDictionary<string, int> expectedTokens,
        IReadOnlyDictionary<string, int> actualTokens)
    {
        var missingTokens = GetTokenDifferences(expectedTokens, actualTokens);
        var extraTokens = GetTokenDifferences(actualTokens, expectedTokens);

        if (missingTokens.Count > 0)
        {
            errors.Add($"{translationKey}: missing {tokenType}{Plural(missingTokens.Count)} {FormatTokens(missingTokens)}. {guidance}");
        }

        if (extraTokens.Count > 0)
        {
            errors.Add($"{translationKey}: unexpected {tokenType}{Plural(extraTokens.Count)} {FormatTokens(extraTokens)}. {guidance}");
        }
    }

    private static Dictionary<string, int> CountMatches(string value, Regex regex)
    {
        return CountMatches(value, regex, match => match.Value);
    }

    private static Dictionary<string, int> CountMatches(string value, Regex regex, Func<Match, string> selector)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match match in regex.Matches(value))
        {
            var token = selector(match);
            counts[token] = counts.GetValueOrDefault(token) + 1;
        }

        return counts;
    }

    private static Dictionary<string, int> GetTokenDifferences(
        IReadOnlyDictionary<string, int> left,
        IReadOnlyDictionary<string, int> right)
    {
        var differences = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (token, count) in left)
        {
            var difference = count - right.GetValueOrDefault(token);
            if (difference > 0)
            {
                differences[token] = difference;
            }
        }

        return differences;
    }

    private static string NormalizeHtmlTag(string value)
    {
        return HtmlTagNameRegex().Replace(value, match =>
        {
            var slash = match.Groups["slash"].Success ? "/" : string.Empty;
            var name = match.Groups["name"].Value.ToLowerInvariant();
            return $"<{slash}{name}>";
        });
    }

    private static string FormatTokens(IReadOnlyDictionary<string, int> tokens)
    {
        return string.Join(", ", tokens.Select(token => token.Value == 1 ? token.Key : $"{token.Key} x{token.Value}"));
    }

    private static string Plural(int count) => count == 1 ? string.Empty : "s";

    [GeneratedRegex(@"\{\d+(?::[^{}]*)?\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"<\s*/?\s*[A-Za-z][A-Za-z0-9:-]*(?:\s+[^<>]*)?>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"^<\s*(?<slash>/)?\s*(?<name>[A-Za-z][A-Za-z0-9:-]*)(?:\s+[^<>]*)?>$", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagNameRegex();

    [GeneratedRegex(@"&(?:amp;)?lt;\s*/?\s*[A-Za-z][A-Za-z0-9:-]*(?:\s+[^&]*)?&(?:amp;)?gt;", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EncodedHtmlTagRegex();
}
