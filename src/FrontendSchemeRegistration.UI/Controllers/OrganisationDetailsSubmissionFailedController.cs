namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Constants;
using Extensions;
using Microsoft.AspNetCore.Mvc;
using ViewModels;

[Route(PagePaths.OrganisationDetailsSubmissionFailed)]
public class OrganisationDetailsSubmissionFailedController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return View(
            "OrganisationDetailsSubmissionFailed",
            new OrganisationDetailsSubmissionFailedViewModel
            {
                IsComplianceScheme = User.GetUserData().Organisations[0].OrganisationRole == OrganisationRoles.ComplianceScheme
            });
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        return RedirectToAction("Get", "FileUploadSubLanding");
    }
}