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
        [Route("select-prns")]
        public async Task<IActionResult> SelectPrnsToAcceptOrReject()
        {
            var prnService = new PrnService();
            var prns = prnService.GetPrns();
            return View(prns);
        }

        [HttpGet]
        [Route("prn-accept")]
        public async Task<IActionResult> AcceptPRN()
        {
            var prnService = new PrnService();
            var prnAccept = prnService.GetAcceptPrn();
            return View(prnAccept);
        }

        [HttpPost]
        [Route("accept-prns")]
        public async Task<ActionResult> AcceptPrns(PrnListViewModel selections)
        {
            var bla = selections;
            return View();
        }
    }
}