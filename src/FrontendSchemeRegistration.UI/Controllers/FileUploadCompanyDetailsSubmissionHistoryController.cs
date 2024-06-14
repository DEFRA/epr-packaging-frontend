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
    [Route(PagePaths.FileUploadCompanyDetailsSubmissionHistory)]
    public class FileUploadCompanyDetailsSubmissionHistoryController : Controller
    {
        private readonly ISubmissionService _submissionService;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public FileUploadCompanyDetailsSubmissionHistoryController(
            ISubmissionService submissionService,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _submissionService = submissionService;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Get()
        {
            var organisationId = User.GetUserData().Organisations.First().Id.Value;
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var complianceSchemaId = session.RegistrationSession.SelectedComplianceScheme?.Id;

            var submissionIds = await _submissionService.GetSubmissionIdsAsync(
                organisationId,
                SubmissionType.Registration,
                complianceSchemaId,
                null);

            var viewModel = new FileUploadCompanyDetailsSubmissionHistoryViewModel
            {
                PreviousSubmissionHistoryExists = false,
                SubmissionPeriods = new List<FileUploadCompanyDetailsSubmissionHistoryPeriodViewModel>()
            };

            foreach (var submissionId in submissionIds)
            {
                var submissionHistory = await _submissionService.GetSubmissionHistoryAsync(
                       submissionId.SubmissionId,
                       new DateTime(submissionId.Year, 1, 1));

                if (submissionHistory.Count > 0)
                {
                    var extractYear = submissionId.SubmissionPeriod.ToStartEndDate();

                    viewModel.SubmissionPeriods.Add(new FileUploadCompanyDetailsSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionId.SubmissionPeriod,
                        DatePeriodStartMonth = submissionId.LocalisedMonth(MonthType.Start),
                        DatePeriodEndMonth = submissionId.LocalisedMonth(MonthType.End),
                        DatePeriodYear = extractYear.Start.Year.ToString(),
                        SubmissionHistory = submissionHistory
                    });
                }
            }

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");

            return View("FileUploadCompanyDetailsSubmissionHistory", viewModel);
        }
    }
}
