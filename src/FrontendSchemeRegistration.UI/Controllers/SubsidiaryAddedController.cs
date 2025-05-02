using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    [Route(PagePaths.SubsidiaryAdded)]
    public class SubsidiaryAddedController : Controller
    {
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public SubsidiaryAddedController(
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

            if (session?.SubsidiarySession == null)
            {
                return RedirectToAction("Get", "SubsidiaryCompaniesHouseNumber");
            }

            var subsidiary = new SubsidiaryAddedViewModel
            {
                OrganisationId = session.SubsidiarySession.Company?.OrganisationId,
                OrganisationName = session.SubsidiarySession.Company?.Name
            };

            return View("SubsidiaryAdded", subsidiary);
        }
    }
}
