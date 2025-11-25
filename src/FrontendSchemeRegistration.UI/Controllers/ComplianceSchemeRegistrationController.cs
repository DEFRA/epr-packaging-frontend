using EPR.Common.Authorization.Constants;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.CsoRegistration)]
public class ComplianceSchemeRegistrationController() : Controller
{
    [HttpGet]
    public Task<IActionResult> ComplianceSchemeRegistration(string nation)
    {
        var csoRegViewModel = new ComplianceSchemeRegistrationViewModel("foo cso", nation);
        
        return Task.FromResult<IActionResult>(View(csoRegViewModel));
    }
}