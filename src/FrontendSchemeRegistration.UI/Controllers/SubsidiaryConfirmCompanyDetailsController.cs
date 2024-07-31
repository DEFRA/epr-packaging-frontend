using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Route(PagePaths.SubsidiaryConfirmCompanyDetails)]
public class SubsidiaryConfirmCompanyDetailsController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public SubsidiaryConfirmCompanyDetailsController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session?.SubsidiarySession is null)
        {
            return RedirectToAction("Get", "SubsidiaryCompaniesHouseNumber");
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");

        var viewModel = new SubsidiaryConfirmCompanyDetailsViewModel
        {
            CompanyName = session.SubsidiarySession.Company?.Name,
            CompaniesHouseNumber = session.SubsidiarySession.Company?.CompaniesHouseNumber,
            BusinessAddress = session.SubsidiarySession.Company?.BusinessAddress
        };

        return View("SubsidiaryConfirmCompanyDetails", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Post(SubsidiaryConfirmCompanyDetailsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");
            return View("SubsidiaryConfirmCompanyDetails", model);
        }

        return RedirectToAction("Get", "SubsidiaryLocation");
    }
}