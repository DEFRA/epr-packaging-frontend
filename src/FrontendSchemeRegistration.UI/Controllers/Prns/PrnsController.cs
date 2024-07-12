using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class PrnsController : Controller
    {
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
            var prnService = new PrnService();
            var prns = prnService.GetAllPrns();
            return View(prns);
        }

        [HttpGet]
        [Route("select-prns")]
        public async Task<IActionResult> SelectPrnsToAcceptOrReject()
        {
            var prnService = new PrnService();
            var prns = prnService.GetPrnsAwaitingAcceptance();
            return View(prns);
        }

        [HttpGet]
        [Route("accept-prn")]
        public async Task<IActionResult> AcceptSinglePrn([FromQuery]string prnOrPernNumber)
        {
            return View();
        }

        [HttpPost]
        [Route("accept-prns")]
        public async Task<ActionResult> AcceptPrns(PrnListViewModel model)
        {
            var selections = model.Prns.Where(x => x.IsSelected);
            if (selections.Count() == 1)
            {
                return View(selections);
            }

            return View();
        }
    }
}