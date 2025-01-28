using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.Controllers.Prns;

[FeatureGate(FeatureFlags.ShowPrn)]
[ServiceFilter(typeof(ComplianceSchemeIdHttpContextFilterAttribute))]
public class PrnsController : Controller
{
    private const string ShowPrnPageName = "SelectSinglePrn";
    private const string NoPrnsSelected = "NoPrnsSelected";
    private const string FileName = "PRNDetail.csv";
    private const string MimeType = "text/csv";
    private readonly IPrnService _prnService;
    private readonly IDownloadPrnService _downloadPrnService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public PrnsController(IPrnService prnService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
                          IDownloadPrnService downloadPrnService)
    {
        _prnService = prnService;
        _sessionManager = sessionManager;
        _downloadPrnService = downloadPrnService;
    }

	[HttpGet]
	[Route(PagePaths.Prns.Search)]
	public async Task<IActionResult> SearchPrns([FromQuery] SearchPrnsViewModel request)
	{
		var prnsResultViewModel = await _prnService.GetPrnSearchResultsAsync(request);

        // stop, there are zero matching PRNs
        if (prnsResultViewModel?.ActivePageOfResults.Count == 0)
        {
            var newRequest = new SearchPrnsViewModel();
            if (JsonConvert.SerializeObject(request) == JsonConvert.SerializeObject(newRequest))
            {
                return View("SearchPrnsEmpty");
            }

            var allResults = await _prnService.GetPrnSearchResultsAsync(newRequest);
            if (allResults?.ActivePageOfResults.Count == 0)
            {
                return View("SearchPrnsEmpty");
            }
        }

        if (request.Source == "button" && string.IsNullOrWhiteSpace(request.Search))
		{
				ViewData.ModelState.AddModelError("search", "enter_prn_or_pern_number");
		}

		var qs = $"?search={request.Search}&sortBy={request.SortBy}&filterBy={request.FilterBy}&page=";

        if (prnsResultViewModel != null)
        {
            prnsResultViewModel.PagingDetail.PagingLink = Url.Action(nameof(SearchPrns)) + qs;
        }

        await SetSessionBackLinkForShowPrnPage(Url.Action(nameof(SearchPrns)) + qs + request.Page);
        ViewBag.BackLinkToDisplay = Url.Content($"~/{PagePaths.Prns.ObligationsHome}");

        return View(prnsResultViewModel);
    }

    // Select single or multiple Prns to accept or reject. Step 1 of 5 choose PRN(s) from list
    [HttpGet]
    [HttpPost]
    [Route(PagePaths.Prns.ShowAwaitingAcceptance)]
    public async Task<IActionResult> SelectMultiplePrns(SearchPrnsViewModel request)
    {
        if (TempData[NoPrnsSelected] != null)
        {
            ViewData.ModelState.AddModelError("Error", TempData[NoPrnsSelected].ToString());
        }

        if (string.IsNullOrEmpty(request.FilterBy))
        {
            request.FilterBy = PrnConstants.Filters.AwaitingAll;
        }

        var awaitingAcceptanceViewModel = await _prnService.GetPrnAwaitingAcceptanceSearchResultsAsync(request);

        // stop, there are zero PRNs awaiting acceptance
        if ((awaitingAcceptanceViewModel.Prns.Count == 0) && (request.FilterBy == PrnConstants.Filters.AwaitingAll))
        {
            return View("SelectMultiplePrnsEmpty");
        }

		await SetSessionBackLinkForShowPrnPage(Url?.Content(string.Concat("~/", PagePaths.Prns.ShowAwaitingAcceptance)));
        ViewBag.BackLinkToDisplay = Url.Content($"~/{PagePaths.Prns.ObligationsHome}");

        awaitingAcceptanceViewModel.SelectedFilter = request.FilterBy;
        awaitingAcceptanceViewModel.SelectedSort = request.SortBy;
        var qs = $"?sortBy={request.SortBy}&filterBy={request.FilterBy}&page=";
        awaitingAcceptanceViewModel.PagingDetail.PagingLink = Url.Action(nameof(SelectMultiplePrns)) + qs;
        return View(awaitingAcceptanceViewModel);
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
        return View(prn);
    }

    [HttpGet]
    [Route(PagePaths.Prns.DownloadSelectedPRNPdf + "/{id:guid}")]
    public async Task<IActionResult> DownloadPrn(Guid id)
    {
        var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor, ModelState);
        return await _downloadPrnService.DownloadPrnAsync(id, "SelectSinglePrn", actionContext);
    }

    [HttpGet]
    [Route(PagePaths.Prns.DownloadAllPRNsAndPERNs)]
    public async Task<IActionResult> DownloadPrnsToCsv()
    {
        var stream = await _prnService.GetPrnsCsvStreamAsync();

        // stop, there are zero PRNs to download
        if (stream.Length == 0)
        {
            return View("CsvEmpty");
        }

        return File(stream, MimeType, FileName);
    }

    private async Task SetSessionBackLinkForShowPrnPage(string backLink)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PrnSession.Backlinks[ShowPrnPageName] = backLink;
        _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}