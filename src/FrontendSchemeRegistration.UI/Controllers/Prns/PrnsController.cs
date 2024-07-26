using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class PrnsController : Controller
    {
        private readonly IPrnService _prnService;

        public PrnsController(IPrnService prnService)
        {
            _prnService = prnService;
        }

        // Step 1 choose PRN from list
        [HttpGet]
        [Route("view-awaiting-acceptance")]
        public async Task<IActionResult> ViewAllPrns()
        {
            var prns = await _prnService.GetAllPrnsAsync();
            return View(prns);
        }

        // Step 2, show more details of PRN
        [HttpGet]
        [Route("selected-prn/{id}")]
        public async Task<IActionResult> AcceptSinglePrn(Guid id)
        {
            var prn = await _prnService.GetPrnByExternalIdAsync(id);
            return View(prn);
        }

        // Step 3, are you sure?
        [HttpGet]
        [Route("accept-prn/{id}")]
        public async Task<IActionResult> ConfirmAcceptSinglePrn(Guid id)
        {
            var prn = await _prnService.GetPrnByExternalIdAsync(id);
            return View(prn);
        }

        // Step 4 update PRN to accpted
        [HttpPost]
        [Route("confirm-accept-prn")]
        public async Task<ActionResult> SetPrnStatusToAccepted(PrnViewModel model)
        {
            await _prnService.AcceptPrnAsync(model.ExternalId);

            return RedirectToAction(nameof(PrnsController.AcceptedPrn), "Prns", new { id = model.ExternalId });
        }

        // Step 5 show updated PRN details
        [HttpGet]
        [Route("accepted-prn/{id}")]
        public async Task<IActionResult> AcceptedPrn(Guid id)
        {
            var model = await _prnService.GetPrnByExternalIdAsync(id);

            if (model == null || !model.ApprovalStatus.EndsWith("ACCEPTED"))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), "Prns");
            }

            return View(model);
        }

        // Landing page
        [HttpGet]
        [Route("manage-prn-home-complete")]
        public async Task<IActionResult> HomePagePrn()
        {
            return View();
        }

        [HttpGet]
        [Route("view-awaiting-acceptance-alt")]
        public async Task<IActionResult> SelectPrnsToAcceptOrReject()
        {
            var prns = await _prnService.GetPrnsAwaitingAcceptanceAsync();
            return View(prns);
        }

        [HttpPost]
        [Route("accept-bulk")]
        public async Task<ActionResult> AcceptPrns(PrnListViewModel model)
        {
            var selections = model.Prns.Where(x => x.IsSelected).Select(x => x.Id);
            if (selections.Any())
            {
                return View(selections);
            }

            return View();
        }

        [HttpPost]
        [Route("RejectPRN")]
        public async Task<ActionResult> RejectPRN(PrnViewModel model)
        {
            return RedirectToAction(nameof(PrnsController.ViewAllPrns));
        }
    }
}