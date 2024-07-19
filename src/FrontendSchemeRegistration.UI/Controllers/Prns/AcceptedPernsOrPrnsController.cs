using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
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

        [HttpPost]
        [Route("accepted-prn")]
        public IActionResult AcceptedPernsOrPrns(PrnViewModel model)
        {
            if (model == null || !model.ApprovalStatus.EndsWith("ACCEPTED"))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), "Prns");
            }

            return View("Views/Prns/AcceptedPernsOrPrns.cshtml", model);
        }

        [HttpGet]
        [Route("accepted-prn/{id}")]
        public IActionResult AcceptedPernsOrPrns(int id)
        {
            var model = _prnService.GetPrnById(id);

            return AcceptedPernsOrPrns(model);
        }
    }
}
