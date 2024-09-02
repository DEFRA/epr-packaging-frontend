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

        private readonly string _basePath;

        public FileUploadSubsidiariesController(
            IFileUploadService fileUploadService,
            ISubmissionService submissionService,
            ISubsidiaryService subsidiaryService,
            IOptions<GlobalVariables> globalVariables,
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
            IComplianceSchemeMemberService complianceSchemeMemberService)
        {
            _fileUploadService = fileUploadService;
            _submissionService = submissionService;
            _subsidiaryService = subsidiaryService;
            _basePath = globalVariables.Value.BasePath;
            _sessionManager = sessionManager;
            _complianceSchemeMemberService = complianceSchemeMemberService;
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

            return View(vm);
        }

        [HttpPost]
        [Route(PagePaths.FileUploadSubsidiaries)]
        [FeatureGate(FeatureFlags.ShowSubsidiariesFileUploadExportRemoveFeature)]
        public async Task<IActionResult> Post()
        {
            var submissionId = await _fileUploadService.ProcessUploadAsync(
                Request.ContentType,
                Request.Body,
                ModelState,
                null,
                SubmissionType.Subsidiary,
                null);

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
                IsFileUploadTakingLong = submission.SubsidiaryFileUploadDateTime <= DateTime.Now.AddMinutes(-5),
            };
            return submission.SubsidiaryDataComplete || submission.Errors.Count > 0
                ? RedirectToAction(nameof(FileUploadSuccess), new RouteValueDictionary { { "recordsAdded", submission.RecordsAdded } })
                : View("FileUploading", subFileUploadViewModel);
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiariesSuccess)]
        public async Task<IActionResult> FileUploadSuccess()
        {
            var model = new SubsidiaryFileUploadSuccessViewModel();

            model.RecordsAdded = int.TryParse(Request.Query["recordsAdded"], out var recordsAdded) ? recordsAdded : 0;

            return View("FileUploadSuccess", model);
        }

        [HttpGet]
        [Route(PagePaths.ExportSubsidiaries)]
        public async Task<IActionResult> ExportSubsidiaries(int subsidiaryParentId)
        {
            var userData = User.GetUserData();
            var organisation = userData.Organisations[0];
            bool isComplienceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
            var stream = await _subsidiaryService.GetSubsidiariesStreamAsync(subsidiaryParentId, isComplienceScheme);

            return File(stream, "text/csv", $"subsidiary.csv");
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
                                Name = response.Organisation.Name,
                                Id = response.Organisation.OrganisationNumber,
                                CompaniesHouseNumber = response.Organisation.CompaniesHouseNumber,
                                Subsidiaries = response.Relationships.Select(r => new SubsidiaryViewModel(r.OrganisationNumber, r.OrganisationName, r.CompaniesHouseNumber, r.OldSubsidiaryId)).ToList()
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
                                Name = c.OrganisationName,
                                Id = c.OrganisationNumber,
                                CompaniesHouseNumber = c.CompaniesHouseNumber,
                                Subsidiaries = c.Relationships.Select(s => new SubsidiaryViewModel(s.OrganisationNumber, s.OrganisationName, s.CompaniesHouseNumber, s.OldSubsidiaryId)).ToList()
                            }).ToList()
                    };
                }
            }

            ViewBag.BackLinkToDisplay = _basePath;

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
    }
}