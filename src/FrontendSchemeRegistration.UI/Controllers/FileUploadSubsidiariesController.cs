namespace FrontendSchemeRegistration.UI.Controllers
{
    using Application.Constants;
    using Application.DTOs.Submission;
    using Application.Enums;
    using Application.Options;
    using Application.Services.Interfaces;
    using Constants;
    using EPR.Common.Authorization.Constants;
    using EPR.Common.Authorization.Models;
    using EPR.Common.Authorization.Sessions;
    using Extensions;
    using global::FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
    using global::FrontendSchemeRegistration.UI.Attributes.ActionFilters;
    using global::FrontendSchemeRegistration.UI.Services.FileUploadLimits;
    using global::FrontendSchemeRegistration.UI.Services.Messages;
    using global::FrontendSchemeRegistration.UI.Sessions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.FeatureManagement;
    using Microsoft.FeatureManagement.Mvc;
    using Services.Interfaces;
    using ViewModels;

    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [ComplianceSchemeIdActionFilter]
    public class FileUploadSubsidiariesController : Controller
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ISubmissionService _submissionService;
        private readonly ISubsidiaryService _subsidiaryService;
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
        private readonly IComplianceSchemeMemberService _complianceSchemeMemberService;
        private readonly IComplianceSchemeService _complianceSchemeService;
        private readonly ISubsidiaryUtilityService _subsidiaryUtilityService;
        private readonly IFeatureManager _featureManager;
        private IOptions<GlobalVariables> _globalVariables;

        private readonly string _basePath;

        public FileUploadSubsidiariesController(
            IFileUploadService fileUploadService,
            ISubmissionService submissionService,
            ISubsidiaryService subsidiaryService,
            IOptions<GlobalVariables> globalVariables,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
            IComplianceSchemeMemberService complianceSchemeMemberService,
            IComplianceSchemeService complianceSchemeService,
            ISubsidiaryUtilityService subsidiaryUtilityService,
            IFeatureManager featureManager)
        {
            _fileUploadService = fileUploadService;
            _submissionService = submissionService;
            _subsidiaryService = subsidiaryService;
            _basePath = globalVariables.Value.BasePath;
            _sessionManager = sessionManager;
            _complianceSchemeMemberService = complianceSchemeMemberService;
            _complianceSchemeService = complianceSchemeService;
            _subsidiaryUtilityService = subsidiaryUtilityService;
            _globalVariables = globalVariables;
            _featureManager = featureManager;
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiaries)]
        [FeatureGate(FeatureFlags.ShowSubsidiaries)]
        public async Task<IActionResult> SubsidiariesList([FromQuery] int? page = 1)
        {
            
            if (page < 1)
            {
                return RedirectToAction(nameof(SubsidiariesList), new { page = 1 });
            }
            
            var vm = await GetSubsidiaryListViewModel(page);
            if (page > vm.PagingDetail.TotalItems)
            {
                page -= 1;
                vm = await GetSubsidiaryListViewModel(page);
            }
            var (userId, organisationId) = GetUserDetails();
            
            if (!await _subsidiaryService.GetSubsidiaryFileUploadStatusViewedAsync(userId, organisationId))
            {
                var fileUploadStatus = await _subsidiaryService.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);
                switch (fileUploadStatus)
                {
                    case SubsidiaryFileUploadStatus.FileUploadedSuccessfully:
                        return RedirectToAction(nameof(FileUploadSuccess));
                    case SubsidiaryFileUploadStatus.HasErrors:
                        return RedirectToAction(nameof(SubsidiariesFileNotUploaded));
                    case SubsidiaryFileUploadStatus.PartialUpload:
                        return RedirectToAction(nameof(SubsidiariesIncompleteFileUpload));
                    case SubsidiaryFileUploadStatus.FileUploadInProgress:
                        vm.IsFileUploadInProgress = true;
                        break;

                    default:
                        break;
                }
            }
                        
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            SetBackLink(session, vm.IsFileUploadInProgress);
            await SavePageNumberToSession(session, page.Value);
            return View(vm);
        }
          


        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiariesCheckStatus)]
        public async Task<JsonResult> CheckFileUploadSatus()
        {
            var (userId, organisationId) = GetUserDetails();
            var fileUploadStatus = await _subsidiaryService.GetSubsidiaryFileUploadStatusAsync(userId,organisationId);
            return fileUploadStatus switch
            {
                SubsidiaryFileUploadStatus.FileUploadedSuccessfully =>
                    Json(new { redirectUrl = Url.Action(nameof(FileUploadSuccess)) }),

                SubsidiaryFileUploadStatus.HasErrors =>
                    Json(new { redirectUrl = Url.Action(nameof(SubsidiariesFileNotUploaded)) }),

                SubsidiaryFileUploadStatus.PartialUpload =>
                    Json(new { redirectUrl = Url.Action(nameof(SubsidiariesIncompleteFileUpload)) }),

                _ =>
                    Json(new { isFileUploadInProgress = true })
            };
        }


        [HttpPost]
        [Route(PagePaths.FileUploadSubsidiaries)]
        [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
        public async Task<IActionResult> Post()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            SetBackLink(session);

            var submissionId = await _fileUploadService.ProcessUploadAsync(
                Request.ContentType,
                Request.Body,
                ModelState,
                null,
                SubmissionType.Subsidiary,
                new SubsidiaryFileUploadMessages(),
                new SubsidiariesFileUploadLimit(_globalVariables),
                session.RegistrationSession.SelectedComplianceScheme?.Id);

            var routeValues = new RouteValueDictionary { { "submissionId", submissionId } };

            return !ModelState.IsValid
                ? View("SubsidiariesList", await GetSubsidiaryListViewModel(1))
                : RedirectToAction(nameof(FileUploading), routeValues);
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesUploadingAndValidatingFile)]
        [SubmissionIdActionFilter(PagePaths.FileUploadSubsidiaries)]
        public async Task<IActionResult> FileUploading()
        {
            var submissionId = Guid.Parse(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<SubsidiarySubmission>(submissionId);

            if (submission is null)
            {
                return RedirectToAction(nameof(SubsidiariesList));
            }

            var (userId, organisationId) = GetUserDetails();
            await _subsidiaryService.SetSubsidiaryFileUploadStatusViewedAsync(false, userId, organisationId);

            var userData = User.GetUserData();

            var uploadStatus = await _subsidiaryService.GetUploadStatus(userData.Id.Value, userData.Organisations[0].Id.Value);

            if (uploadStatus.Status == SubsidiaryUploadStatus.Uploading)
            {
                var subFileUploadViewModel = new SubFileUploadingViewModel()
                {
                    SubmissionId = submissionId.ToString(),
                    IsFileUploadTakingLong = submission.SubsidiaryFileUploadDateTime <= DateTime.UtcNow.AddMinutes(-5),
                };

                return View("FileUploading", subFileUploadViewModel);
            }

            if (uploadStatus.Errors != null)
            {
                return uploadStatus.RowsAdded == 0
                    ? RedirectToAction(nameof(SubsidiariesFileNotUploaded))
                    : RedirectToAction(nameof(SubsidiariesIncompleteFileUpload));
            }

            return RedirectToAction(nameof(FileUploadSuccess));
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiariesSuccess)]
        public async Task<IActionResult> FileUploadSuccess()
        {
            var (userId, organisationId) = GetUserDetails();
            var uploadStatus = await _subsidiaryService.GetUploadStatus(userId,organisationId);
            var userData = User.GetUserData();
            var organisation = userData.Organisations[0];
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var selectedComplienceSchemeId = session.RegistrationSession?.SelectedComplianceScheme?.Id;
            var totalSubsidiariesCount = await _subsidiaryUtilityService.GetSubsidiariesCount(organisation.OrganisationRole, organisationId, selectedComplienceSchemeId);
            await _subsidiaryService.SetSubsidiaryFileUploadStatusViewedAsync(true, userId, organisationId);
            await SaveSession(session, PagePaths.FileUploadSubsidiariesSuccess);
            
            var model = new SubsidiaryFileUploadSuccessViewModel
            {
                RecordsAdded = uploadStatus?.RowsAdded ?? 0,
                TotalSubsidiariesCount = totalSubsidiariesCount,
                ShowTotalSubsidiariesCount = (totalSubsidiariesCount - (uploadStatus?.RowsAdded ?? 0)) > 0,
            };

            return View("FileUploadSuccess", model);
        }

               
        [HttpGet]
        [Route(PagePaths.SubsidiariesIncompleteFileUpload)]
        public async Task<IActionResult> SubsidiariesIncompleteFileUpload()
        {
            var (userId, organisationId) = GetUserDetails();
            await _subsidiaryService.SetSubsidiaryFileUploadStatusViewedAsync(true, userId, organisationId);
            return await GetViewForUnsuccessfulFileUpload(true);
        }

        [HttpPost]
        [Route(PagePaths.SubsidiariesIncompleteFileUpload)]
        public async Task<IActionResult> SubsidiariesIncompleteFileUploadDecision([FromForm] SubsidiaryUnsuccessfulUploadDecisionViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return await GetViewForUnsuccessfulFileUpload(true);
            }

            return GetRedirectForUnsuccessfulFileUploadDecision(viewModel);
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesFileNotUploaded)]
        public async Task<IActionResult> SubsidiariesFileNotUploaded()
        {
            var (userId, organisationId) = GetUserDetails();
            await _subsidiaryService.SetSubsidiaryFileUploadStatusViewedAsync(true, userId, organisationId);
            return await GetViewForUnsuccessfulFileUpload(false);
        }

        [HttpPost]
        [Route(PagePaths.SubsidiariesFileNotUploaded)]
        public async Task<IActionResult> SubsidiariesFileNotUploadedDecision([FromForm] SubsidiaryUnsuccessfulUploadDecisionViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return await GetViewForUnsuccessfulFileUpload(false);
            }

            return GetRedirectForUnsuccessfulFileUploadDecision(viewModel);
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesFileUploadWarningsReport)]
        public async Task<IActionResult> SubsidiariesFileUploadWarningsReport()
        {
            var userData = User.GetUserData();

            var reportStream = await _subsidiaryService.GetUploadErrorsReport(userData.Id.Value, userData.Organisations[0].Id.Value);

            return File(reportStream, "text/csv", "subsidiary_validation_report.csv");
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesDownload)]
        public async Task<IActionResult> SubsidiariesDownload()
        {
            TempData["DownloadCompleted"] = false;
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.SubsidiariesDownload);

            return RedirectToAction(nameof(SubsidiariesDownloadView), "FileUploadSubsidiaries");
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesDownloadView)]
        public IActionResult SubsidiariesDownloadView()
        {
            return View(nameof(SubsidiariesDownload));
        }

        [HttpGet]
        [Route(PagePaths.SubsidiariesDownloadFailed)]
        public async Task<IActionResult> SubsidiariesDownloadFailed()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.SubsidiariesDownloadFailed);

            return View(nameof(SubsidiariesDownloadFailed));
        }

        [HttpGet]
        [Route(PagePaths.ExportSubsidiaries)]
        public async Task<IActionResult> ExportSubsidiaries()
        {
            try
            {
                var userData = User.GetUserData();
                var organisation = userData.Organisations[0];
                var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
                var complianceSchemeId = session.RegistrationSession?.SelectedComplianceScheme?.Id;
                var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
                var includeSubsidiaryJoinerAndLeaverColumns = await _featureManager.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerAndLeaverColumns);

                var stream = await _subsidiaryService.GetSubsidiariesStreamAsync(organisation.Id.Value, complianceSchemeId, isComplianceScheme, includeSubsidiaryJoinerAndLeaverColumns);

                if (stream == null)
                {
                    return RedirectToAction(nameof(SubsidiariesDownloadFailed));
                }

                TempData["DownloadCompleted"] = true;
                return File(stream, "text/csv", "subsidiary.csv");

            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(SubsidiariesDownloadFailed));
            }
        }

        [HttpPost]
        [Route(PagePaths.ConfirmSubsidiaryRemoval + "/{SubsidiaryReference}")]
        public async Task<IActionResult> ConfirmRemoveSubsidiary(SubsidiaryConfirmRemovalViewModel model)
        {
            switch (model.SelectedConfirmRemoval)
            {
                case YesNoAnswer.Yes:
                    TempData["SubsidiaryNameToRemove"] = model.SubsidiaryName;
                    var userId = HttpContext.User.GetUserData().Id.Value;
                    await _subsidiaryService.TerminateSubsidiary(model.ParentOrganisationExternalId, model.SubsidiaryExternalId, userId);
                    return RedirectToAction(nameof(ConfirmRemoveSubsidiarySuccess));
                case YesNoAnswer.No:
                    var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
                    var pageToReturnTo = session.SubsidiarySession.ReturnToSubsidiaryPage;
                    return RedirectToAction(nameof(SubsidiariesList), new { page = pageToReturnTo });
            }

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubsidiaries}");

            return View(model);
        }


        [HttpGet]
        [Route(PagePaths.ConfirmSubsidiaryRemoval + "/{subsidiaryReference}")]
        public async Task<IActionResult> ConfirmRemoveSubsidiary(string subsidiaryReference, Guid parentOrganisationExternalId)
        {
            var subsidiaryDetails = await _subsidiaryService.GetOrganisationByReferenceNumber(subsidiaryReference);

            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SubsidiaryReference = subsidiaryReference,
                SubsidiaryName = subsidiaryDetails.Name,
                SubsidiaryExternalId = subsidiaryDetails.ExternalId,
                ParentOrganisationExternalId = parentOrganisationExternalId,
            };
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.ConfirmSubsidiaryRemoval);

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubsidiaries}");

            return View(model);
        }

        [HttpGet]
        [Route(PagePaths.ConfirmRemoveSubsidiarySuccess)]
        public async Task<IActionResult> ConfirmRemoveSubsidiarySuccess()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.ConfirmRemoveSubsidiarySuccess);
            var model = new ConfirmRemoveSubsidiarySuccessViewModel
            {
                SubsidiaryName = TempData["SubsidiaryNameToRemove"]?.ToString(),
                ReturnToSubsidiaryPage = session.SubsidiarySession.ReturnToSubsidiaryPage
            };
            return View(model);
        }

        private (Guid userId, Guid organisationId) GetUserDetails()
        {
            var user = User.GetUserData();
            return (user.Id.Value, user.Organisations[0].Id.Value);
        }
        
        private async Task<SubsidiaryListViewModel> GetSubsidiaryListViewModel(int? page)
        {
            const int showPerPage = 1;

            var userData = User.GetUserData();
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            var organisation = userData.Organisations[0];
            var isDirectProducer = organisation.OrganisationRole == OrganisationRoles.Producer;
            var pageCount = 1;
            SubsidiaryListViewModel result;

            if (isDirectProducer)
            {
                var response = await _subsidiaryService.GetOrganisationSubsidiaries(organisation.Id.Value);
                if (response == null || response.Relationships.Count == 0)
                {
                    result = GetEmptySubsidiaryListViewModel(organisation);
                }
                else
                {
                    result = new SubsidiaryListViewModel
                    {
                        Organisations =
                        [
                            new()
                            {
                                ExternalId = response.Organisation.Id,
                                Name = response.Organisation.Name,
                                Id = response.Organisation.OrganisationNumber,
                                CompaniesHouseNumber = response.Organisation.CompaniesHouseNumber,
                                Subsidiaries = response.Relationships.Select(r => new SubsidiaryViewModel(r.OrganisationNumber, r.OrganisationName, r.CompaniesHouseNumber, r.JoinerDate, r.ReportingType)).ToList()
                            }
                        ]
                    };
                }
            }
            else
            {
                var complianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;
                var complianceSchemeMembershipResponse = await _complianceSchemeMemberService.GetComplianceSchemeMembers(organisation.Id.Value, complianceSchemeId, showPerPage, string.Empty, page.Value, true);
                if (complianceSchemeMembershipResponse.PagedResult == null || complianceSchemeMembershipResponse.PagedResult.Items.Count == 0)
                {
                    result = GetEmptySubsidiaryListViewModel(new Organisation() { });
                }
                else
                {
                    pageCount = complianceSchemeMembershipResponse.PagedResult.TotalItems;
                    result = new SubsidiaryListViewModel
                    {
                        Organisations = complianceSchemeMembershipResponse.PagedResult.Items.Select(c =>
                            new SubsidiaryOrganisationViewModel
                            {
                                ExternalId = c.SelectedSchemeOrganisationExternalId,
                                Name = c.OrganisationName,
                                Id = c.OrganisationNumber,
                                CompaniesHouseNumber = c.CompaniesHouseNumber,
                                Subsidiaries = c.Relationships.Select(s => new SubsidiaryViewModel(s.OrganisationNumber, s.OrganisationName, s.CompaniesHouseNumber, s.JoinerDate, s.ReportingType)).ToList()
                            }).ToList()
                    };
                }

                var currentSummary = await _complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, complianceSchemeId);
                result.MemberCount = currentSummary.MemberCount;
            }
            
            var pageUrl = Url.Action(nameof(SubsidiariesList));

            result.PagingDetail = new PagingDetail
            {
                CurrentPage = page.Value,
                PageSize = showPerPage,
                TotalItems = pageCount,
                PagingLink = $"{pageUrl}?page="
            };
            result.IsDirectProducer = isDirectProducer;
            return result;
        }

        private static SubsidiaryListViewModel GetEmptySubsidiaryListViewModel(Organisation organisation)
        {
            return new SubsidiaryListViewModel
            {
                Organisations =
                [
                    new()
                    {
                        Name = organisation.Name,
                        Id = organisation.OrganisationNumber,
                        Subsidiaries = new List<SubsidiaryViewModel>()
                    }

                ]
            };
        }

        private RedirectToActionResult GetRedirectForUnsuccessfulFileUploadDecision(SubsidiaryUnsuccessfulUploadDecisionViewModel viewModel)
        {
            return viewModel.UploadNewFile == true
                ? RedirectToAction("SubsidiariesList", new { page = 1 })
                : RedirectToAction("Get", "Landing");
        }

        private async Task<ViewResult> GetViewForUnsuccessfulFileUpload(bool partialSucess)
        {
            var userData = User.GetUserData();

            var reportStream = await _subsidiaryService.GetUploadErrorsReport(userData.Id.Value, userData.Organisations[0].Id.Value);

            var viewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = partialSucess,
                WarningsReportDisplaySize = reportStream.Length switch
                {
                    >= 1048576 => $"{reportStream.Length / 1048576}MB",
                    >= 1024 => $"{reportStream.Length / 1024}KB",
                    _ => $"{reportStream.Length}B"
                }
            };

            return View("SubsidiariesUnsuccessfulFileUpload", viewModel);
        }

        private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath)
        {
            session.SubsidiarySession.Journey.Clear();
            session.SubsidiarySession.Journey.AddIfNotExists(currentPagePath);

            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }
        
        private async Task SavePageNumberToSession(FrontendSchemeRegistrationSession session, int currentPage)
        {
            session.SubsidiarySession.ReturnToSubsidiaryPage = currentPage;
            
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }

        private void SetBackLink(FrontendSchemeRegistrationSession session,bool fileUploadInProgress = false)
        {
            var pageFrom = session.SubsidiarySession.Journey.LastOrDefault();
            if (ShouldShowAccountHomeLink(pageFrom))
            {
                ViewBag.ShouldShowAccountHomeLink = true;
                ViewBag.BackLinkToDisplay = string.Empty;
            }
            else
            {
                ViewBag.BackLinkToDisplay = _basePath;
                ViewBag.ShouldShowAccountHomeLink = false;
            }

            if (fileUploadInProgress)
            {
                ViewBag.BackLinkToDisplay = string.Empty;
                ViewBag.ShouldShowAccountHomeLink = false;
            }
        
        }
        
        private static bool ShouldShowAccountHomeLink(string previousPage)
        {
            return previousPage is PagePaths.FileUploadSubsidiariesSuccess 
                or PagePaths.SubsidiariesDownload 
                or PagePaths.SubsidiariesDownloadFailed
                or PagePaths.ConfirmSubsidiaryRemoval
                or PagePaths.ConfirmRemoveSubsidiarySuccess;
        }
    }
}