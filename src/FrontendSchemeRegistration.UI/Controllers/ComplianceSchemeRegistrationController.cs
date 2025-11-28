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
        var complianceSchemesTask = complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);
        var registrationApplicationPerYearViewModelsTask = registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);
        await Task.WhenAll(complianceSchemesTask, registrationApplicationPerYearViewModelsTask);
        var cso = complianceSchemesTask.Result.Single(cs => cs.NationId == (int)nation);

        var csoRegViewModel = new ComplianceSchemeRegistrationViewModel(cso.Name, nation.ToString(), registrationApplicationPerYearViewModelsTask.Result, 2026);
        
        return View(csoRegViewModel);
    }
}