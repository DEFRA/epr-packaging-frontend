namespace FrontendSchemeRegistration.UI.Controllers.Culture;

using Application.Constants;
using Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class CultureController : Controller
{
    [HttpGet]
    [Route(PagePaths.Culture)]
    public LocalRedirectResult UpdateCulture(string culture, string returnUrl)
    {
        HttpContext.Session.SetString(Language.SessionLanguageKey, culture);
        return LocalRedirect(returnUrl);
    }
}
