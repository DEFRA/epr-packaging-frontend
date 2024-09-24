using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.Prns
{
    [FeatureGate(FeatureFlags.ShowPrn)]
    public class PrnsAcceptController : Controller
    {
        private readonly IPrnService _prnService;
        private readonly TimeProvider _timeProvider;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public PrnsAcceptController(IPrnService prnService, TimeProvider timeProvider, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _prnService = prnService;
            _timeProvider = timeProvider;
            _sessionManager = sessionManager;
        }

        // Note steps 1 and 2 are in the PrnsController
        // Accept single Prn. Step 3 of 5, are you sure?
        [HttpGet]
        [Route(PagePaths.Prns.AskToAccept + "/{id:guid}")]
        public async Task<IActionResult> AcceptSinglePrn(Guid id)
        {
            var prn = await _prnService.GetPrnByExternalIdAsync(id);
            if (prn.DecemberWasteRulesApply(_timeProvider.GetUtcNow().Date) && prn.IssueYear.Equals(2024))
            {
                return View("AcceptSingle2024DecemberWastePrn", prn);
            }

            return View("AcceptSinglePrn", prn);
        }

        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.ConfirmAccept)]
        public async Task<ActionResult> ConfirmAcceptSinglePrnPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept single Prn. Step 4 of 5, update PRN to accpted
        [HttpPost]
        [Route(PagePaths.Prns.ConfirmAccept)]
        public async Task<ActionResult> ConfirmAcceptSinglePrnPassThrough(PrnViewModel model)
        {
            await _prnService.AcceptPrnAsync(model.ExternalId);

            return RedirectToAction(nameof(PrnsAcceptController.AcceptedPrn), nameof(PrnsAcceptController).RemoveControllerFromName(), new { id = model.ExternalId });
        }

        // Accept single Prn. Step 5 of 5, show updated PRN details following acceptance
        [HttpGet]
        [Route(PagePaths.Prns.Accepted + "/{id:guid}")]
        public async Task<IActionResult> AcceptedPrn(Guid id)
        {
            var model = await _prnService.GetPrnByExternalIdAsync(id);

            if (model == null || !model.ApprovalStatus.EndsWith(PrnStatus.Accepted))
            {
                return RedirectToAction(nameof(PrnsController.HomePagePrn), nameof(PrnsController).RemoveControllerFromName());
            }

            return View(model);
        }

        // Note step 1 is in the PrnsController
        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.BeforeAskToAcceptMany)]
        public async Task<ActionResult> AcceptMultiplePrnsPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept multiple Prns. Step 2 of 5 remember chosen PRN ids
        [HttpPost]
        [Route(PagePaths.Prns.BeforeAskToAcceptMany)]
        public async Task<ActionResult> AcceptMultiplePrnsPassThrough(PrnListViewModel model)
        {
            var selectedPrns= model?.Prns?.Where(x => x.IsSelected);
            if (selectedPrns != null && selectedPrns.Any())
            {               
                var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
                session.PrnSession = new PrnSession();
                session.PrnSession.SelectedPrnIds = selectedPrns.Select(x => x.ExternalId).ToList();
                session.PrnSession.InitialNoteTypes = model.GetPluralNoteType(selectedPrns);
                await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

                return RedirectToAction(nameof(PrnsAcceptController.AcceptMultiplePrns));
            }

            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName(), new { error = "zero-accepted" });
        }

        // Accept multiple Prns. Step 3 of 5 display chosen PRNs with option to deselect
        [HttpGet]
        [Route(PagePaths.Prns.AskToAcceptMany + "/{id?}")]
        public async Task<ActionResult> AcceptMultiplePrns(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
            List<Guid> selectedPrnIds = session.PrnSession.SelectedPrnIds;
            ViewBag.StartNoteTypes = session.PrnSession.InitialNoteTypes;
            PrnListViewModel viewModel = await _prnService.GetPrnsAwaitingAcceptanceAsync();

            if (id != Guid.Empty)
            {
                var removedPrn = viewModel.Prns.First(x => x.ExternalId == id);
                viewModel.RemovedPrn = new RemovedPrn(removedPrn.PrnOrPernNumber, removedPrn.IsPrn);
                
                selectedPrnIds.Remove(id);         
                session.PrnSession.SelectedPrnIds = selectedPrnIds.ToList();
                await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
            }

            var selectedPrns = viewModel?.Prns?.Where(x => selectedPrnIds.Contains(x.ExternalId)).OrderBy(x => x.Material).ThenByDescending(x => x.DateIssued);
            if (selectedPrns != null)
            {
                viewModel.Prns = selectedPrns.ToList();
            }
            return View(viewModel);
        }

        // Unexpected hit, assume user has timed out and been redirected here after relogin
        [HttpGet]
        [Route(PagePaths.Prns.ConfirmAcceptMany)]
        public async Task<ActionResult> ConfirmAcceptMultiplePrnsPassThrough()
        {
            return RedirectToAction(nameof(PrnsController.SelectMultiplePrns), nameof(PrnsController).RemoveControllerFromName());
        }

        // Accept multiple Prns. Step 4 of 5 update PRNs to accpted
        [HttpPost]
        [Route(PagePaths.Prns.ConfirmAcceptMany)]
        public async Task<ActionResult> ConfirmAcceptMultiplePrnsPassThrough(PrnListViewModel model)
        {
            var selectedPrnIds = model.Prns.Select(x => x.ExternalId);
            await _prnService.AcceptPrnsAsync(selectedPrnIds.ToArray());
            return RedirectToAction(nameof(PrnsAcceptController.AcceptedPrns));
        }

        // Accept multiple Prns. Step 5 of 5 display newly accepted PRN summary details
        [HttpGet]
        [Route(PagePaths.Prns.AcceptedMany)]
        public async Task<IActionResult> AcceptedPrns()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
            List<Guid> selectedPrnIds = session.PrnSession.SelectedPrnIds;

            var viewModel = await _prnService.GetAllAcceptedPrnsAsync();
            var justUpdatedPrns = viewModel.Prns?.Where(x => selectedPrnIds.Contains(x.ExternalId)).ToList();

            var summaryModel = new AcceptedPrnsModel()
            {
                NoteTypes = viewModel.GetNoteType(justUpdatedPrns),
                Count = justUpdatedPrns.Count,
                Details = justUpdatedPrns
                            .GroupBy(x => x.Material)
                            .Select(x => new AcceptedDetails(x.Key, x.Sum(t => t.Tonnage)))
                            .ToList()
            };

            return View(summaryModel);
        }
    }
}