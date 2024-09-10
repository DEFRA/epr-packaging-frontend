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
    public class PrnsRejectController : Controller
    {
        private readonly IPrnService _prnService;

        public PrnsRejectController(IPrnService prnService)
        {
            _prnService = prnService;
        }

        // Reject single Prn. Step 3 of 5, are you sure?
        [HttpGet]
        [Route(PagePaths.Prns.AskToReject + "/{id:guid}")]
        public async Task<IActionResult> RejectSinglePrn(Guid id)
        {
            var prn = await _prnService.GetPrnByExternalIdAsync(id);

            return View("RejectSinglePrn", prn);
        }

        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.ConfirmReject)]
        public async Task<ActionResult> ConfirmRejectSinglePrnPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Reject single Prn. Step 4 of 5 update PRN to rejected
        [HttpPost]
        [Route(PagePaths.Prns.ConfirmReject)]
        public async Task<ActionResult> ConfirmRejectSinglePrnPassThrough(PrnViewModel model)
        {
            await _prnService.RejectPrnAsync(model.ExternalId);

            return RedirectToAction(nameof(PrnsRejectController.RejectedPrn), nameof(PrnsRejectController).RemoveControllerFromName(), new { id = model.ExternalId });
        }

        // Reject single Prn. Step 5 of 5 show updated PRN details following rejection
        [HttpGet]
        [Route(PagePaths.Prns.Rejected + "/{id:guid}")]
        public async Task<IActionResult> RejectedPrn(Guid id)
        {
            var model = await _prnService.GetPrnByExternalIdAsync(id);

            if (model == null || !model.ApprovalStatus.Equals(PrnStatus.Rejected))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), nameof(PrnsController).RemoveControllerFromName());
            }

            return View(model);
        }
    }
}
