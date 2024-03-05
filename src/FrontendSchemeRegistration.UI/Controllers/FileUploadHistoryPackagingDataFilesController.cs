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
    [Route($"{PagePaths.FileUploadHistoryPackagingDataFiles}-{{year}}")]
    public class FileUploadHistoryPackagingDataFilesController : Controller
    {
        private readonly ISubmissionService _submissionService;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public FileUploadHistoryPackagingDataFilesController(
            ISubmissionService submissionService,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _submissionService = submissionService;
            _sessionManager = sessionManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int year)
        {
            if (year < 2000 || DateTime.Now.Year <= year)
            {
                return RedirectToAction(
                    nameof(FileUploadNoSubmissionHistoryController.Get),
                    nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
            }

            var organisationId = User.GetOrganisationId().Value;
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var complienceSchemaId = session.RegistrationSession.SelectedComplianceScheme?.Id;
            var yearStart = new DateTime(year, 1, 1);

            var submissionIds = await _submissionService.GetSubmissionIdsAsync(
                organisationId,
                SubmissionType.Producer,
                complienceSchemaId,
                yearStart.Year);

            var viewModel = new FileUploadHistoryPackagingDataFilesViewModel
            {
                Year = yearStart.Year,
                SubmissionPeriods = new List<FileUploadSubmissionHistoryPeriodViewModel>()
            };

            foreach (var submissionId in submissionIds)
            {
                var submissionHistory = await _submissionService.GetSubmissionHistoryAsync(submissionId.SubmissionId, yearStart);

                if (submissionHistory.Count > 0)
                {
                    viewModel.SubmissionPeriods.Add(new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionId.SubmissionPeriod,
                        SubmissionHistory = submissionHistory
                    });
                }
            }

            if (viewModel.SubmissionPeriods.Count == 0)
            {
                return RedirectToAction(
                    nameof(FileUploadNoSubmissionHistoryController.Get),
                    nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
            }

            return View("FileUploadHistoryPackagingDataFiles", viewModel);
        }
    }
}
