using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate("ShowPrn")]
    public class AcceptedPrnController : Controller
    {
        private readonly IPrnService _prnService;

        public AcceptedPrnController(IPrnService prnService)
        {
            _prnService = prnService;
        }

        [HttpGet]
        [Route("accepted-prn/{id}")]
        public IActionResult AcceptedPrn(int id)
        {
            var model = _prnService.GetPrnById(id);

            if (model == null || !model.ApprovalStatus.EndsWith("ACCEPTED"))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), "Prns");
            }

            return View("Views/Prns/AcceptedPrn.cshtml", model);
        }
    }
}
