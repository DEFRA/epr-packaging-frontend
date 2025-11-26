using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Enums;
using Application.Services.Interfaces;
using Extensions;
using Services;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.CsoRegistration)]
public class ComplianceSchemeRegistrationController(
    IComplianceSchemeService complianceSchemeService,
    IRegistrationApplicationService registrationApplicationService
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> ComplianceSchemeRegistration([FromQuery]Nation nation)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var complianceSchemes = await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);
        var cso = complianceSchemes.Single(cs => cs.NationId == (int)nation);
        var registrationApplicationPerYearViewModels = await registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);

        var csoRegViewModel = new ComplianceSchemeRegistrationViewModel(cso.Name, nation.ToString(), registrationApplicationPerYearViewModels, 2026);
        
        return View(csoRegViewModel);
    }
}