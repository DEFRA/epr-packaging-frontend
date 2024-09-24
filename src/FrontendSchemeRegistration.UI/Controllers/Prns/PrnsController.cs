using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns;

[FeatureGate(FeatureFlags.ShowPrn)]
public class PrnsController : Controller
{
    private const string ShowPrnPageName = "SelectSinglePrn";
    private readonly IPrnService _prnService;
    private readonly TimeProvider _timeProvider;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public PrnsController(IPrnService prnService, TimeProvider timeProvider, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _prnService = prnService;
        _timeProvider = timeProvider;
        _sessionManager = sessionManager;
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
	[Route(PagePaths.Prns.Search)]
	public async Task<IActionResult> SearchPrns([FromQuery] SearchPrnsViewModel request)
	{
		var prnsResultViewModel = await _prnService.GetPrnSearchResultsAsync(request);

		if (request.Source == "button")
		{
			if (string.IsNullOrWhiteSpace(request.Search))
			{
				ViewData.ModelState.AddModelError("search", "enter_the_exact_prn_or_pern_number");
			}
			else if (prnsResultViewModel.ActivePageOfResults.Count == 0)
			{
				ViewData.ModelState.AddModelError("search", "no_prns_or_perns_found");
			}
		}

		var qs = string.IsNullOrWhiteSpace(request.Search) ? "?page=" : "?search=" + request.Search + "&page=";
		prnsResultViewModel.PagingDetail.PagingLink = Url.Action(nameof(SearchPrns)) + qs;

        await SetSessionBackLinkForShowPrnPage(Url.Action(nameof(SearchPrns)) + qs + request.Page);

        return View(prnsResultViewModel);
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

        await SetSessionBackLinkForShowPrnPage(Url?.Content(string.Concat("~/", PagePaths.Prns.ShowAwaitingAcceptance)));

        var prns = await _prnService.GetPrnsAwaitingAcceptanceAsync();
        return View(prns);
    }

    // Accept or reject single Prn. Step 2 of 5 for single PRN not multiple PRNS, show details of PRN
    [HttpGet]
    [Route(PagePaths.Prns.ShowSelected + "/{id:guid}")]
    public async Task<IActionResult> SelectSinglePrn(Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var backLink = session.PrnSession.Backlinks[ShowPrnPageName];
        ViewBag.BackLinkToDisplay = (backLink != null)? backLink.ToString(): Url?.Content(string.Concat("~/", PagePaths.Prns.ShowAwaitingAcceptance));

        var prn = await _prnService.GetPrnByExternalIdAsync(id);
        ViewBag.DecemberWasteRulesApply = prn.DecemberWasteRulesApply(_timeProvider.GetUtcNow().DateTime);
        return View(prn);
    }

    private async Task SetSessionBackLinkForShowPrnPage(string backLink)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PrnSession.Backlinks[ShowPrnPageName] = backLink;
        _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}