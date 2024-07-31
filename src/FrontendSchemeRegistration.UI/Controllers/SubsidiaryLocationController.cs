using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.SubsidiaryLocation)]
public class SubsidiaryLocationController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public SubsidiaryLocationController(ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");

        var viewModel = new SubsidiaryLocationViewModel()
        {
            UkNation = session.SubsidiarySession?.UkNation
        };

        return View("SubsidiaryLocation", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Post(SubsidiaryLocationViewModel model)
    {
        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");

        if (!ModelState.IsValid)
        {
            if (model.UkNation == null)
            {
                var errorMessage = "uk-nation-errormessage";
                ModelState.ClearValidationState(nameof(model.UkNation));
                ModelState.AddModelError(nameof(model.UkNation), errorMessage);
            }

            return View("SubsidiaryLocation", model);
        }

        if (model.UkNation.HasValue)
        {
            _sessionManager.UpdateSessionAsync(HttpContext.Session, x => x.SubsidiarySession.UkNation = model.UkNation);
            return RedirectToAction("Get", "SubsidiaryCheckDetails");
        }

        return View("SubsidiaryLocation", model);
    }
}
