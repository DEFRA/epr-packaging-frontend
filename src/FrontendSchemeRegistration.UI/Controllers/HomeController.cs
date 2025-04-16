namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

[AllowAnonymous]
public class HomeController : Controller
{
    [Route(PagePaths.SignedOut)]
    public IActionResult SignedOut()
    {
        HttpContext.Session.Clear();
        return View();
    }

    [Route(PagePaths.TimeoutSignedOut)]
    public IActionResult TimeoutSignedOut()
    {
        HttpContext.Session.Clear();
        return View("TimeoutSignedOut");
    }

    public IActionResult SessionTimeoutModal()
    {
        return PartialView("_TimeoutSessionWarning");
    }
}