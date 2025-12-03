using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

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
        var complianceSchemesTask = complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);
        var registrationApplicationPerYearViewModelsTask = registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);
        await Task.WhenAll(complianceSchemesTask, registrationApplicationPerYearViewModelsTask);
        var cso = complianceSchemesTask.Result.Single(cs => cs.NationId == (int)nation);

        ViewBag.BackLinkToDisplay = PagePaths.ComplianceSchemeLanding;

        var csoRegViewModel = new ComplianceSchemeRegistrationViewModel(cso.Name, nation.ToString(), registrationApplicationPerYearViewModelsTask.Result, 2026);
        
        return View(csoRegViewModel);
    }
}