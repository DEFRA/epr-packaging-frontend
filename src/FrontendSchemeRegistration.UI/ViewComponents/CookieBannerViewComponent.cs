namespace FrontendSchemeRegistration.UI.ViewComponents;

using Application.Constants;
using Application.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ViewModels.Shared;

public class CookieBannerViewComponent : ViewComponent
{
    private readonly CookieOptions _options;

    public CookieBannerViewComponent(IOptions<CookieOptions> options)
    {
        _options = options.Value;
    }

    public IViewComponentResult Invoke(string returnUrl)
    {
        var consentCookie = Request.Cookies[_options.CookiePolicyCookieName];

        var cookieAcknowledgement = TempData[CookieAcceptance.CookieAcknowledgement]?.ToString();

        var dontShowBanner = ViewContext.RouteData.Values["controller"]?.ToString() == "Cookies";

        var cookieBannerModel = new CookieBannerModel
        {
            CurrentPage = Request.Path,
            ShowBanner = !dontShowBanner && cookieAcknowledgement == null && consentCookie == null,
            ShowAcknowledgement = !dontShowBanner && cookieAcknowledgement != null,
            AcceptAnalytics = cookieAcknowledgement == CookieAcceptance.Accept,
            ReturnUrl = $"{returnUrl}{Request.QueryString}",
        };

        return View(cookieBannerModel);
    }
}
