namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Parses the composite bearer token produced by StubTokenAcquisition.
/// Token format: {userId}::{email}   (email part is optional).
/// </summary>
[ExcludeFromCodeCoverage]
public static class StubToken
{
    private const string Separator = "::";

    public static string? ExtractRawToken(WireMock.IRequestMessage req)
    {
        if (req.Headers != null && req.Headers.TryGetValue("Authorization", out var header))
        {
            var authHeader = header.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }
        }

        return null;
    }

    public static string? ExtractUserId(WireMock.IRequestMessage req)
    {
        var raw = ExtractRawToken(req);
        return ExtractUserId(raw);
    }

    public static string? ExtractUserId(string? rawToken)
    {
        if (string.IsNullOrEmpty(rawToken))
            return null;

        var idx = rawToken.IndexOf(Separator, StringComparison.Ordinal);
        return idx >= 0 ? rawToken[..idx] : rawToken;
    }

    public static string? ExtractEmail(WireMock.IRequestMessage req)
    {
        var raw = ExtractRawToken(req);
        return ExtractEmail(raw);
    }

    public static string? ExtractEmail(string? rawToken)
    {
        if (string.IsNullOrEmpty(rawToken))
            return null;

        var idx = rawToken.IndexOf(Separator, StringComparison.Ordinal);
        return idx >= 0 ? rawToken[(idx + Separator.Length)..] : null;
    }

    /// <summary>
    /// Returns true when the email part of the token contains the given keyword (case-insensitive).
    /// </summary>
    public static bool EmailContains(WireMock.IRequestMessage req, string keyword)
    {
        var email = ExtractEmail(req);
        return !string.IsNullOrEmpty(email)
               && email.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    public enum UserType
    {
        ComplianceScheme,
        DirectProducer,
        CsMemberProducer
    }

    /// <summary>
    /// Resolves the user type from the email keyword. Defaults to ComplianceScheme if no keyword is found.
    /// Falls back to UserId-based matching against the legacy GUID constants.
    /// </summary>
    public static UserType ResolveUserType(WireMock.IRequestMessage req)
    {
        var email = ExtractEmail(req);
        if (!string.IsNullOrEmpty(email))
        {
            if (email.Contains("producer", StringComparison.OrdinalIgnoreCase) &&
                !email.Contains("csmember", StringComparison.OrdinalIgnoreCase))
                return UserType.DirectProducer;

            if (email.Contains("csmember", StringComparison.OrdinalIgnoreCase))
                return UserType.CsMemberProducer;
        }

        return UserType.ComplianceScheme;
    }

    private static readonly (string Keyword, int NationId, string NationName, string Country)[] NationKeywords =
    [
        ("scotland", 3, "Scotland", "Scotland"),
        ("wales", 4, "Wales", "Wales"),
        ("northernireland", 2, "NorthernIreland", "Northern Ireland"),
    ];

    /// <summary>
    /// Resolves the nation from the email keyword. Defaults to England (1) if no keyword is found.
    /// </summary>
    public static (int NationId, string NationName, string Country) ResolveNation(WireMock.IRequestMessage req)
    {
        var email = ExtractEmail(req);
        if (!string.IsNullOrEmpty(email))
        {
            foreach (var (keyword, nationId, nationName, country) in NationKeywords)
            {
                if (email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return (nationId, nationName, country);
            }
        }

        return (1, "England", "England");
    }
}
