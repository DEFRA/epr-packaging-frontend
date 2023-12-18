namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

public class CookieBannerModel
{
    public bool ShowBanner { get; set; }

    public bool ShowAcknowledgement { get; set; }

    public bool AcceptAnalytics { get; set; }

    public string? ReturnUrl { get; set; }

    public string CurrentPage { get; set; }
}