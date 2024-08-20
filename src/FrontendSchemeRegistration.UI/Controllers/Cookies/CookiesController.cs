namespace FrontendSchemeRegistration.UI.Controllers.Cookies;

using Application.Constants;
using Application.Options;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ViewModels;

[AllowAnonymous]
public class CookiesController : Controller
{
    private readonly ICookieService _cookieService;
    private readonly CookieOptions _eprCookieOptions;
    private readonly GoogleAnalyticsOptions _googleAnalyticsOptions;

    public CookiesController(
        ICookieService cookieService,
        IOptions<CookieOptions> eprCookieOptions,
        IOptions<GoogleAnalyticsOptions> googleAnalyticsOptions)
    {
        _cookieService = cookieService;
        _eprCookieOptions = eprCookieOptions.Value;
        _googleAnalyticsOptions = googleAnalyticsOptions.Value;
    }

    [Route(PagePaths.Cookies)]
    public IActionResult Detail(string returnUrl, bool? cookiesAccepted = null)
    {
        var allowedBackValues = new List<string> { "/report-data", "/create-account", "/manage-account" };
        var validBackLink = !string.IsNullOrWhiteSpace(returnUrl) && allowedBackValues.Exists(a => returnUrl.StartsWith(a));

        string returnUrlAddress = validBackLink ? returnUrl : Url.Content("~/");

        var hasUserAcceptedCookies = cookiesAccepted != null ? cookiesAccepted.Value : _cookieService.HasUserAcceptedCookies(Request.Cookies);

        var cookieViewModel = new CookieDetailViewModel
        {
            SessionCookieName = _eprCookieOptions.SessionCookieName,
            CookiePolicyCookieName = _eprCookieOptions.CookiePolicyCookieName,
            AntiForgeryCookieName = _eprCookieOptions.AntiForgeryCookieName,
            GoogleAnalyticsDefaultCookieName = _googleAnalyticsOptions.DefaultCookieName,
            GoogleAnalyticsAdditionalCookieName = _googleAnalyticsOptions.AdditionalCookieName,
            AuthenticationCookieName = _eprCookieOptions.AuthenticationCookieName,
            TsCookieName = _eprCookieOptions.TsCookieName,
            TempDataCookieName = _eprCookieOptions.TempDataCookie,
            B2CCookieName = _eprCookieOptions.B2CCookieName,
            CorrelationCookieName = _eprCookieOptions.CorrelationCookieName,
            OpenIdCookieName = _eprCookieOptions.OpenIdCookieName,
            CookiesAccepted = hasUserAcceptedCookies,
            ReturnUrl = returnUrlAddress,
            ShowAcknowledgement = cookiesAccepted != null
        };

        ViewBag.BackLinkToDisplay = returnUrlAddress;
        ViewBag.CurrentPage = returnUrlAddress;

        return View(cookieViewModel);
    }

    [HttpPost]
    [Route(PagePaths.Cookies)]
    public IActionResult Detail(string returnUrl, string cookiesAccepted)
    {
        _cookieService.SetCookieAcceptance(cookiesAccepted == CookieAcceptance.Accept, Request.Cookies, Response.Cookies);
        TempData[CookieAcceptance.CookieAcknowledgement] = cookiesAccepted;

        return Detail(returnUrl, cookiesAccepted == CookieAcceptance.Accept);
    }

    [HttpPost]
    [Route(PagePaths.UpdateCookieAcceptance)]
    public LocalRedirectResult UpdateAcceptance(string returnUrl, string cookies)
    {
        _cookieService.SetCookieAcceptance(cookies == CookieAcceptance.Accept, Request.Cookies, Response.Cookies);
        TempData[CookieAcceptance.CookieAcknowledgement] = cookies;

        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    [Route(PagePaths.AcknowledgeCookieAcceptance)]
    public LocalRedirectResult AcknowledgeAcceptance(string returnUrl)
    {
        return LocalRedirect(returnUrl);
    }
}