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
    using global::FrontendSchemeRegistration.UI.Attributes.ActionFilters;
    using global::FrontendSchemeRegistration.UI.Services.FileUploadLimits;
    using global::FrontendSchemeRegistration.UI.Services.Messages;
    using global::FrontendSchemeRegistration.UI.Sessions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
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
        private IOptions<GlobalVariables> _globalVariables;

        private readonly string _basePath;

        public FileUploadSubsidiariesController(
            IFileUploadService fileUploadService,
            ISubmissionService submissionService,
            ISubsidiaryService subsidiaryService,
            IOptions<GlobalVariables> globalVariables,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
            IComplianceSchemeMemberService complianceSchemeMemberService
            )
        {
            _fileUploadService = fileUploadService;
            _submissionService = submissionService;
            _subsidiaryService = subsidiaryService;
            _basePath = globalVariables.Value.BasePath;
            _sessionManager = sessionManager;
            _complianceSchemeMemberService = complianceSchemeMemberService;
            _globalVariables = globalVariables;
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
                return new NotFoundResult();
            }
            
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            SetBackLink(session);
            
            return View(vm);
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
        [Route("FileUploading")]
        public async Task<IActionResult> FileUploading()
        {
            var submissionId = Guid.Parse(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<SubsidiarySubmission>(submissionId);

            if (submission is null)
            {
                return RedirectToAction("Get", "FileUpload");
            }

            var subFileUploadViewModel = new SubFileUploadingViewModel()
            {
                SubmissionId = submissionId.ToString(),
                IsFileUploadTakingLong = submission.SubsidiaryFileUploadDateTime <= DateTime.UtcNow.AddMinutes(-5),
            };
            return submission.SubsidiaryDataComplete || submission.Errors.Count > 0
                ? RedirectToAction(nameof(FileUploadSuccess), new RouteValueDictionary { { "recordsAdded", submission.RecordsAdded } })
                : View("FileUploading", subFileUploadViewModel);
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiariesSuccess)]
        public async Task<IActionResult> FileUploadSuccess()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.FileUploadSubsidiariesSuccess);
            
            var model = new SubsidiaryFileUploadSuccessViewModel
            {
                RecordsAdded = int.TryParse(Request.Query["recordsAdded"], out var recordsAdded) ? recordsAdded : 0
            };

            return View("FileUploadSuccess", model);
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
                var stream = await _subsidiaryService.GetSubsidiariesStreamAsync(organisation.Id.Value, complianceSchemeId, isComplianceScheme);

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
        [Route(PagePaths.ConfirmSubsidiaryRemoval)]
        public async Task<IActionResult> ConfirmRemoveSubsidiary(SubsidiaryConfirmRemovalViewModel model)
        {
            switch (model.SelectedConfirmRemoval)
            {
                case YesNoAnswer.Yes:
                    var userId = HttpContext.User.GetUserData().Id.Value;
                    await _subsidiaryService.TerminateSubsidiary(model.ParentOrganisationExternalId, model.SubsidiaryExternalId, userId);
                    return RedirectToAction(nameof(SubsidiariesList));
                case YesNoAnswer.No:
                    return RedirectToAction(nameof(SubsidiariesList));
            }

            return View(model);
        }


        [HttpGet]
        [Route(PagePaths.ConfirmSubsidiaryRemoval + "/{subsidiaryReference}")]
        public async Task<IActionResult> ConfirmRemoveSubsidiary(string subsidiaryReference, Guid parentOrganisationExternalId)
        {
            var userData = User.GetUserData();

            if (!userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved))
            {
                return new UnauthorizedResult();
            }

            var subsidiaryDetails = await _subsidiaryService.GetOrganisationByReferenceNumber(subsidiaryReference);

            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SubsidiaryName = subsidiaryDetails.Name,
                SubsidiaryExternalId = subsidiaryDetails.ExternalId,
                ParentOrganisationExternalId = parentOrganisationExternalId,
            };
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            await SaveSession(session, PagePaths.ConfirmSubsidiaryRemoval);

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubsidiaries}");

            return View(model);
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
                if (response is null)
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
                                Subsidiaries = response.Relationships.Select(r => new SubsidiaryViewModel(r.OrganisationNumber, r.OrganisationName, r.CompaniesHouseNumber)).ToList()
                            }
                        ]
                    };
                }
            }
            else
            {
                var complianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;
                var complianceSchemeMembershipResponse = await _complianceSchemeMemberService.GetComplianceSchemeMembers(organisation.Id.Value, complianceSchemeId, showPerPage, string.Empty, page.Value);
                if (complianceSchemeMembershipResponse.PagedResult is null || complianceSchemeMembershipResponse.PagedResult.Items is null)
                {
                    result = GetEmptySubsidiaryListViewModel(organisation);
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
                                Subsidiaries = c.Relationships.Select(s => new SubsidiaryViewModel(s.OrganisationNumber, s.OrganisationName, s.CompaniesHouseNumber)).ToList()
                            }).ToList()
                    };
                }
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

        private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath)
        {
            session.SubsidiarySession.Journey.Clear();
            session.SubsidiarySession.Journey.AddIfNotExists(currentPagePath);

            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }

        private void SetBackLink(FrontendSchemeRegistrationSession session)
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
        }
        
        private static bool ShouldShowAccountHomeLink(string previousPage)
        {
            return previousPage is PagePaths.FileUploadSubsidiariesSuccess 
                or PagePaths.SubsidiariesDownload 
                or PagePaths.SubsidiariesDownloadFailed
                or PagePaths.ConfirmSubsidiaryRemoval;
        }
    }
}