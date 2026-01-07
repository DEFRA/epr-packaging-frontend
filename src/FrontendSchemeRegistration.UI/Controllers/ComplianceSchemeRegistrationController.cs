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

using Constants;
using Microsoft.FeatureManagement;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.CsoRegistration)]
public class ComplianceSchemeRegistrationController(
    IComplianceSchemeService complianceSchemeService,
    IRegistrationApplicationService registrationApplicationService,
    IFeatureManager featureManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> ComplianceSchemeRegistration([FromQuery]Nation nation)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var complianceSchemesTask = complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);
        var registrationApplicationYearViewModelsTask = registrationApplicationService.BuildRegistrationYearApplicationsViewModels(HttpContext.Session, organisation);
        var legacyViewModelsPerTask = registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);
        await Task.WhenAll(complianceSchemesTask, registrationApplicationYearViewModelsTask, legacyViewModelsPerTask);
        var cso = complianceSchemesTask.Result.Single(cs => cs.NationId == (int)nation);

        ViewBag.BackLinkToDisplay = PagePaths.ComplianceSchemeLanding;

        var csoRegViewModel = new ComplianceSchemeRegistrationViewModel(cso.Name, nation.ToString(), registrationApplicationYearViewModelsTask.Result, legacyViewModelsPerTask.Result, 2026);
        csoRegViewModel.DisplayCsoSmallProducerRegistration = await featureManager.IsEnabledAsync(FeatureFlags.DisplayCsoSmallProducerRegistration);
        
        return View(csoRegViewModel);
    }
}