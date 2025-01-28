namespace FrontendSchemeRegistration.UI.Controllers;

using global::FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Mvc;

[Route(PagePaths.CannotVerifyOrganisation)]
public class CannotVerifyOrganisationController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");

        return View("CannotVerifyOrganisation");
    }
}
