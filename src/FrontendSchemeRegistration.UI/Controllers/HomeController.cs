namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class HomeController : Controller
{
    [Route(PagePaths.SignedOut)]
    public IActionResult SignedOut()
    {
        HttpContext.Session.Clear();
        return View();
    }
}