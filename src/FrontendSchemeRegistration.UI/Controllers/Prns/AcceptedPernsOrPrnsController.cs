using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class AcceptedPernsOrPrnsController : Controller
    {
        private readonly IPrnService _prnService;

        public AcceptedPernsOrPrnsController(IPrnService prnService)
        {
            _prnService = prnService;
        }

        [HttpGet]
        [Route("accepted-prn")]
        [Route("accepted-pern")]
        public IActionResult AcceptedPernsOrPrns(string prnOrPernNumber)
        {
            var model = _prnService.GetPrn(prnOrPernNumber);

            if (model == null || model.Status != "Accepted")
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), "Prns");
            }

            return View("Views/Prns/AcceptedPernsOrPrns.cshtml", model);
        }

        [HttpGet]
        [Route("download-prn")]
        public IActionResult DownloadPrn(string prnOrPernNumber)
        {
            throw new NotImplementedException("Downlaod functionality is yet to develop");
        }
    }
}
