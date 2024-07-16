using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class PrnsController : Controller
    {
        private readonly PrnService _prnService;

        public PrnsController(PrnService prnService)
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
        [Route("accept-prn/{id}")]
        public async Task<IActionResult> AcceptSinglePrn(int id)
        {
            var prn = _prnService.GetPrnById(id);

            return View(prn);
        }

        [HttpPost]
        [Route("confirm-accept-prn")]
        public async Task<ActionResult> ConfirmAcceptSinglePrn(PrnViewModel model)
        {
            // save row and redirect placeholder
            var bla = model.Id;
            return View(model);
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
    }
}