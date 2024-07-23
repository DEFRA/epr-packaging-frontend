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
            var prns = _prnService.GetAllPrns();
            return View(prns);
        }

        // Step 2, show more details of PRN
        [HttpGet]
        [Route("selected-prn/{id}")]
        public async Task<IActionResult> AcceptSinglePrn(int id)
        {
            var prn = _prnService.GetPrnById(id);

            return View(prn);
        }

        // Step 3, between chosing a PRN and accepting the PRN
        [HttpPost]
        [Route("accept-prn")]
        public async Task<IActionResult> ConfirmAcceptSinglePrn(PrnViewModel model)
        {
            var prn = _prnService.GetPrnById(model.Id);
            return View(prn);
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
            var prns = _prnService.GetPrnsAwaitingAcceptance();
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

        // Step 4 update PRN
        [HttpPost]
        [Route("confirm-accept-prn")]
        public async Task<ActionResult> ConfirmAcceptPrn(PrnViewModel model)
        {
            var prn = _prnService.GetPrnById(model.Id);
            _prnService.UpdatePrnStatus(prn.Id, "ACCEPTED");

            // Save data to the facade updating approval status to ""ACCEPTED"
            return RedirectToAction(nameof(PrnsController.AcceptedPrn), "Prns", new { id = model.Id });
        }

        // Step 5 show updated PRN details
        [HttpGet]
        [Route("accepted-prn/{id}")]
        public IActionResult AcceptedPrn(int id)
        {
            var model = _prnService.GetPrnById(id);

            if (model == null || !model.ApprovalStatus.EndsWith("ACCEPTED"))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), "Prns");
            }

            return View(model);
        }
    }
}