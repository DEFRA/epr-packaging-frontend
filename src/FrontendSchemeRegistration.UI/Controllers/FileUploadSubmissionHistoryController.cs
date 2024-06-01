using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.FileUploadSubmissionHistory)]
    public class FileUploadSubmissionHistoryController : Controller
    {
        private readonly ISubmissionService _submissionService;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public FileUploadSubmissionHistoryController(
            ISubmissionService submissionService,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _submissionService = submissionService;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Get()
        {
            // Developer note: Some of the code below was commented out to implement temporary feature.

            var organisationId = User.GetUserData().Organisations.First().Id.Value;
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var complienceSchemaId = session.RegistrationSession.SelectedComplianceScheme?.Id;
            // var startOfCurrentYear = new DateTime(DateTime.Now.Year, 1, 1);

            var submissionIds = await _submissionService.GetSubmissionIdsAsync(
                organisationId,
                SubmissionType.Producer,
                complienceSchemaId,
                null);

            var viewModel = new FileUploadSubmissionHistoryViewModel
            {
                PreviousSubmissionHistoryExists = false,
                SubmissionPeriods = new List<FileUploadSubmissionHistoryPeriodViewModel>()
            };

            /*foreach (var submissionId in submissionIds)
            {
                if (submissionId.Year == startOfCurrentYear.Year)
                {
                    var submissionHistory = await _submissionService.GetSubmissionHistoryAsync(
                        submissionId.SubmissionId,
                        startOfCurrentYear);

                    if (submissionHistory.Count > 0)
                    {
                        viewModel.SubmissionPeriods.Add(new FileUploadSubmissionHistoryPeriodViewModel
                        {
                            SubmissionPeriod = submissionId.SubmissionPeriod,
                            SubmissionHistory = submissionHistory
                        });
                    }
                }
                else if (!viewModel.PreviousSubmissionHistoryExists)
                {
                    viewModel.PreviousSubmissionHistoryExists = true;
                }
            }*/

            foreach (var submissionId in submissionIds)
            {
                var submissionHistory = await _submissionService.GetSubmissionHistoryAsync(
                       submissionId.SubmissionId,
                       new DateTime(submissionId.Year, 1, 1));

                if (submissionHistory.Count > 0)
                {
                    var extractYear = submissionId.SubmissionPeriod.ToStartEndDate();

                    viewModel.SubmissionPeriods.Add(new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionId.SubmissionPeriod,
                        DatePeriodStartMonth = submissionId.LocalisedMonth(MonthType.Start),
                        DatePeriodEndMonth = submissionId.LocalisedMonth(MonthType.End),
                        DatePeriodYear = extractYear.Start.Year.ToString(),
                        SubmissionHistory = submissionHistory
                    });
                }
            }

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

            return View("FileUploadSubmissionHistory", viewModel);
        }
    }
}
