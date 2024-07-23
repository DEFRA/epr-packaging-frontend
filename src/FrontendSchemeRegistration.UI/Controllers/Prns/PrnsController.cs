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

        [HttpGet]
        [Route("prns-home")]
        public async Task<IActionResult> HomePagePrn()
        {
            return View();
        }

        [HttpGet]
        [Route("view-all-prns")]
        public async Task<IActionResult> ViewAllPrns()
        {
            var prns = _prnService.GetAllPrns();
            return View(prns);
        }

        [HttpGet]
        [Route("select-prns")]
        public async Task<IActionResult> SelectPrnsToAcceptOrReject()
        {
            var prns = _prnService.GetPrnsAwaitingAcceptance();
            return View(prns);
        }

        [HttpGet]
        [Route("confirm-accept-prn/{id}")]
        public async Task<IActionResult> ConfirmAcceptSinglePrn(int id)
        {
            var model = _prnService.GetPrnById(id);
            return View(model);
        }

        [HttpGet]
        [Route("accept-prn/{id}")]
        public async Task<IActionResult> AcceptSinglePrn(int id)
        {
            var prn = _prnService.GetPrnById(id);

            return View(prn);
        }

        [HttpPost]
        [Route("accept-prns")]
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
        [Route("confirm-accept-prn")]
        public async Task<ActionResult> ConfirmAcceptPrn(PrnViewModel model)
        {
            var prn = _prnService.GetPrnById(model.Id);
            _prnService.UpdatePrnStatus(prn.Id, "ACCEPTED");

            // Save data to the facade updating approval status to ""ACCEPTED"
            return RedirectToAction(nameof(AcceptedPrnController.AcceptedPrn), "AcceptedPrn", new { id = model.Id });
        }

        [HttpPost]
        [Route("RejectPRN")]
        public async Task<ActionResult> RejectPRN(PrnViewModel model)
        {
            return RedirectToAction(nameof(PrnsController.ViewAllPrns));
        }
    }
}