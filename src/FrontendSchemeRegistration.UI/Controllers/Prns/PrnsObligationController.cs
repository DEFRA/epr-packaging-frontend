using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns;

[FeatureGate(FeatureFlags.ShowPrn)]
public class PrnsObligationController(ISessionManager<FrontendSchemeRegistrationSession> sessionManager) : Controller
{
    [HttpGet]
    [Route(PagePaths.Prns.ObligationsHome)]
    public async Task<IActionResult> ObligationsHome()
    {
            var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
            var year = DateTime.Now.Year;
            var viewModel = new PrnObligationViewModel();
            if (session.UserData.Organisations.Count > 0)
            {
                viewModel.OrganisationRole = session.UserData.Organisations[0].OrganisationRole;
                viewModel.OrganisationName = session.UserData.Organisations[0].Name;
                viewModel.NationId = session.UserData.Organisations[0].NationId;
                viewModel.CurrentYear = year;
                viewModel.DeadlineYear = year + 1;
            }
            return View(viewModel);
    }
}
