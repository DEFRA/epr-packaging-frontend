using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns;

[FeatureGate(FeatureFlags.ShowPrn)]
public class PrnsController : Controller
{
    private readonly IPrnService _prnService;
    private readonly TimeProvider _timeProvider;

    public PrnsController(IPrnService prnService, TimeProvider timeProvider)
    {
        _prnService = prnService;
        _timeProvider = timeProvider;
    }

    // Landing page
    [HttpGet]
    [Route(PagePaths.Prns.Home)]
    public async Task<IActionResult> HomePagePrn()
    {
        var viewModel = await _prnService.GetPrnsAwaitingAcceptanceAsync();
        return View("HomePagePrn", viewModel);
    }

    [HttpGet]
    [Route(PagePaths.Prns.ShowAll)]
    public async Task<IActionResult> ViewAllPrns()
    {
        var prns = await _prnService.GetAllPrnsAsync();
        return View(prns);
    }

    // Select single or multiple Prns to accept or reject. Step 1 of 5 choose PRN(s) from list
    [HttpGet]
    [Route(PagePaths.Prns.ShowAwaitingAcceptance + "/{error?}")]
    public async Task<IActionResult> SelectMultiplePrns(string? error)
    {
        if (error != null)
        {
            ViewData.ModelState.AddModelError("Error", "select_one_or_more_prns_or_perns_to_accept_them");
        }

        var prns = await _prnService.GetPrnsAwaitingAcceptanceAsync();
        return View(prns);
    }

    // Accept or reject single Prn. Step 2 of 5 for single PRN not multiple PRNS, show details of PRN
    [HttpGet]
    [Route(PagePaths.Prns.ShowSelected + "/{id:guid}")]
    public async Task<IActionResult> SelectSinglePrn(Guid id)
    {
        ViewBag.BackLinkToDisplay = Url?.Content(string.Concat("~/", PagePaths.Prns.ShowAwaitingAcceptance));

        var prn = await _prnService.GetPrnByExternalIdAsync(id);
        ViewBag.DecemberWasteRulesApply = prn.DecemberWasteRulesApply(_timeProvider.GetUtcNow().DateTime);
        return View(prn);
    }
}