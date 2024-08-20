namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Services.Interfaces;
using Constants;
using ControllerExtensions;
using Extensions;
using FrontendSchemeRegistration;
using Microsoft.AspNetCore.Mvc;

[Route("/")]
public class LandingController : Controller
{
    private readonly IComplianceSchemeService _complianceSchemeService;

    public LandingController(IComplianceSchemeService complianceSchemeService)
    {
        _complianceSchemeService = complianceSchemeService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        if (organisation.OrganisationRole == OrganisationRoles.ComplianceScheme)
        {
            return RedirectToAction(
                nameof(ComplianceSchemeLandingController.Get),
                nameof(ComplianceSchemeLandingController).RemoveControllerFromName());
        }

        var producerComplianceScheme = await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id.Value);

        if (producerComplianceScheme is not null)
        {
            return RedirectToAction(
                nameof(ComplianceSchemeMemberLandingController.Get),
                nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName());
        }

        return RedirectToAction(
            nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged),
            nameof(FrontendSchemeRegistrationController).RemoveControllerFromName());
    }
}