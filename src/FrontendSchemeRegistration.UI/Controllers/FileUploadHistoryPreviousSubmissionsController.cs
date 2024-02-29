using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.FileUploadHistoryPreviousSubmissions)]
    public class FileUploadHistoryPreviousSubmissionsController : Controller
    {
        private readonly ISubmissionService _submissionService;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public FileUploadHistoryPreviousSubmissionsController(ISubmissionService submissionService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _submissionService = submissionService;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Get([FromQuery] int? page = 1)
        {
            if (page < 1)
            {
                return RedirectToAction(nameof(Get), new { page = 1 });
            }

            const int showPerPage = 5;
            var organisationId = User.GetUserData().Organisations.First().Id.Value;
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var complienceSchemaId = session.RegistrationSession.SelectedComplianceScheme?.Id;

            var submissionIds = await _submissionService.GetSubmissionIdsAsync(
                organisationId,
                SubmissionType.Producer,
                complienceSchemaId,
                null);

            var allSubmissionYears = submissionIds.Where(x => x.Year != DateTime.Now.Year)
                .Select(x => x.Year)
                .Distinct()
                .ToList();

            if (allSubmissionYears.Count == 0)
            {
                return RedirectToAction(
                    nameof(FileUploadSubmissionHistoryController.Get),
                    nameof(FileUploadSubmissionHistoryController).RemoveControllerFromName());
            }

            var pagingDetail = new PagingDetail
            {
                CurrentPage = (int)page,
                PageSize = showPerPage,
                TotalItems = allSubmissionYears.Count
            };

            if (pagingDetail.PageCount < page)
            {
                return RedirectToAction(nameof(Get), new { page = pagingDetail.PageCount });
            }

            pagingDetail.PagingLink = Url.Action(nameof(Get)) + "?page=";

            var viewModel = new FileUploadHistoryPreviousSubmissionsViewModel
            {
                Years = allSubmissionYears.Skip(((int)page - 1) * showPerPage)
                    .Take(showPerPage)
                    .ToList(),
                PagingDetail = pagingDetail
            };

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubmissionHistory}");

            return View("FileUploadHistoryPreviousSubmissions", viewModel);
        }
    }
}
