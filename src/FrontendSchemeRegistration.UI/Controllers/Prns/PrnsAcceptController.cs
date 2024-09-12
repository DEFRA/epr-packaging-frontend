using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate(FeatureFlags.ShowPrn)]
    public class PrnsAcceptController : Controller
    {
        private const string BulkAcceptPrn = "bulkacceptprn";
        private const string StartNoteTypes = "InitialNoteTypes";
        private readonly IPrnService _prnService;
        private readonly TimeProvider _timeProvider;

        public PrnsAcceptController(IPrnService prnService, TimeProvider timeProvider)
        {
            _prnService = prnService;
            _timeProvider = timeProvider;
        }

        // Note steps 1 and 2 are in the PrnsController
        // Accept single Prn. Step 3 of 5, are you sure?
        [HttpGet]
        [Route(PagePaths.Prns.AskToAccept + "/{id:guid}")]
        public async Task<IActionResult> AcceptSinglePrn(Guid id)
        {
            var prn = await _prnService.GetPrnByExternalIdAsync(id);
            if (prn.DecemberWasteRulesApply(_timeProvider.GetUtcNow().Date) && prn.IssueYear.Equals(2024))
            {
                return View("AcceptSingle2024DecemberWastePrn", prn);
            }

            return View("AcceptSinglePrn", prn);
        }

        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.ConfirmAccept)]
        public async Task<ActionResult> ConfirmAcceptSinglePrnPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept single Prn. Step 4 of 5, update PRN to accpted
        [HttpPost]
        [Route(PagePaths.Prns.ConfirmAccept)]
        public async Task<ActionResult> ConfirmAcceptSinglePrnPassThrough(PrnViewModel model)
        {
            await _prnService.AcceptPrnAsync(model.ExternalId);

            return RedirectToAction(nameof(PrnsAcceptController.AcceptedPrn), nameof(PrnsAcceptController).RemoveControllerFromName(), new { id = model.ExternalId });
        }

        // Accept single Prn. Step 5 of 5, show updated PRN details following acceptance
        [HttpGet]
        [Route(PagePaths.Prns.Accepted + "/{id:guid}")]
        public async Task<IActionResult> AcceptedPrn(Guid id)
        {
            var model = await _prnService.GetPrnByExternalIdAsync(id);

            if (model == null || !model.ApprovalStatus.EndsWith(PrnStatus.Accepted))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), nameof(PrnsController).RemoveControllerFromName());
            }

            return View(model);
        }

        // Note step 1 is in the PrnsController
        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.BeforeAskToAcceptMany)]
        public async Task<ActionResult> AcceptMultiplePrnsPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept multiple Prns. Step 2 of 5 remember chosen PRN ids
        [HttpPost]
        [Route(PagePaths.Prns.BeforeAskToAcceptMany)]
        public async Task<ActionResult> AcceptMultiplePrnsPassThrough(PrnListViewModel model)
        {
            var selectedPrnIds = model?.Prns?.Where(x => x.IsSelected).Select(x => x.ExternalId);
            if (selectedPrnIds != null && selectedPrnIds.Any())
            {
                TempData[BulkAcceptPrn] = string.Join(",", selectedPrnIds.ToList());
                return RedirectToAction(nameof(PrnsAcceptController.AcceptMultiplePrns));
            }

            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName(), new { error = "zero-accepted" });
        }

        // Accept multiple Prns. Step 3 of 5 display chosen PRNs with option to deselect
        [HttpGet]
        [Route(PagePaths.Prns.AskToAcceptMany + "/{id?}")]
        public async Task<ActionResult> AcceptMultiplePrns(Guid id)
        {
            List<Guid> selectedPrnIds = GetPrnIdsFromTempdata(BulkAcceptPrn);
            selectedPrnIds.Remove(id);
            TempData[BulkAcceptPrn] = string.Join(",", selectedPrnIds);

            var viewModel = await _prnService.GetPrnsAwaitingAcceptanceAsync();
            var selectedPrns = viewModel.Prns.Where(x => selectedPrnIds.Contains(x.ExternalId)).OrderBy(x => x.Material).ThenByDescending(x => x.DateIssued).ToList();

            if (id != Guid.Empty)
            {
                var removedPrn = viewModel.Prns.First(x => x.ExternalId == id);
                viewModel.RemovedPrn = new RemovedPrn(removedPrn.PrnOrPernNumber, removedPrn.IsPrn);
            }
            else
            {
                var counts = viewModel.GetCountBreakdown(selectedPrns);

                if (counts.PrnCount > 0 && counts.PernCount > 0)
                {
                    TempData[StartNoteTypes] = PrnConstants.PrnsAndPernsText;
                }
                else if (counts.PrnCount > 0)
                {
                    TempData[StartNoteTypes] = PrnConstants.PrnsText;
                }
                else
                {
                    TempData[StartNoteTypes] = PrnConstants.PernsText;
                }
            }

            viewModel.Prns = selectedPrns.ToList();
            return View(viewModel);
        }

        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.ConfirmAcceptMany)]
        public async Task<ActionResult> ConfirmAcceptMultiplePrnsPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept multiple Prns. Step 4 of 5 update PRNs to accpted
        [HttpPost]
        [Route(PagePaths.Prns.ConfirmAcceptMany)]
        public async Task<ActionResult> ConfirmAcceptMultiplePrnsPassThrough(PrnListViewModel model)
        {
            var selectedPrnIds = model.Prns.Select(x => x.ExternalId);

            await _prnService.AcceptPrnsAsync(selectedPrnIds.ToArray());
            TempData[BulkAcceptPrn] = string.Join(",", selectedPrnIds.ToList());
            return RedirectToAction(nameof(PrnsAcceptController.AcceptedPrns));
        }

        // Accept multiple Prns. Step 5 of 5 display newly accepted PRN summary details
        [HttpGet]
        [Route(PagePaths.Prns.AcceptedMany)]
        public async Task<IActionResult> AcceptedPrns()
        {
            TempData.Keep(BulkAcceptPrn);
            List<Guid> selectedPrnIds = GetPrnIdsFromTempdata(BulkAcceptPrn);

            var viewModel = await _prnService.GetAllAcceptedPrnsAsync();
            var justUpdatedPrns = viewModel.Prns?.Where(x => selectedPrnIds.Contains(x.ExternalId)).ToList();

            var summaryModel = new AcceptedPrnsModel()
            {
                NoteTypes = viewModel.GetNoteType(justUpdatedPrns),
                Count = justUpdatedPrns.Count,
                Details = justUpdatedPrns
                            .GroupBy(x => x.Material)
                            .Select(x => new AcceptedDetails(x.Key, x.Sum(t => t.Tonnage)))
                            .ToList()
            };
            return View(summaryModel);
        }

        private List<Guid> GetPrnIdsFromTempdata(string key)
        {
            TempData.TryGetValue(key, out object o);
            string idsString = o == null ? string.Empty : o.ToString();
            return idsString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.Parse(x)).ToList<Guid>();
        }
    }
}