namespace FrontendSchemeRegistration.UI.ViewModels;

public class CookieDetailViewModel
{
    public bool CookiesAccepted { get; set; }

    public bool ShowAcknowledgement { get; set; }

    public string CookiePolicyCookieName { get; set; }

    public string SessionCookieName { get; set; }

    public string AntiForgeryCookieName { get; set; }

    public string TsCookieName { get; set; }

    public string AuthenticationCookieName { get; set; }

    public string TempDataCookieName { get; set; }

    public string B2CCookieName { get; set; }

    public string CorrelationCookieName { get; set; }

    public string OpenIdCookieName { get; set; }

    public string GoogleAnalyticsDefaultCookieName { get; set; }

    public string GoogleAnalyticsAdditionalCookieName { get; set; }

    public string? ReturnUrl { get; set; }
}