namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CookieOptions
{
    public const string ConfigSection = "Cookie";

    public int CookiePolicyDurationInMonths { get; set; }

    public string SessionCookieName { get; set; }

    public string CookiePolicyCookieName { get; set; }

    public string AntiForgeryCookieName { get; set; }

    public string TsCookieName { get; set; }

    public string CorrelationCookieName { get; set; }

    public string OpenIdCookieName { get; set; }

    public string B2CCookieName { get; set; }

    public string AuthenticationCookieName { get; set; }

    public int AuthenticationExpiryInMinutes { get; set; }

    public string TempDataCookie { get; set; }
}