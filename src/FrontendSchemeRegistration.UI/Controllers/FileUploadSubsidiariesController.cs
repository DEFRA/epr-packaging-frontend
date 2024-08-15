using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    using Application.Options;
    using Constants;
    using Extensions;
    using Microsoft.Extensions.Options;
    using Microsoft.FeatureManagement.Mvc;

    public class FileUploadSubsidiariesController : Controller
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ISubmissionService _submissionService;
        private readonly ISubsidiaryService _subsidiaryService;
        private readonly string _basePath;

        public FileUploadSubsidiariesController(
            IFileUploadService fileUploadService,
            ISubmissionService submissionService,
            ISubsidiaryService subsidiaryService,
            IOptions<GlobalVariables> globalVariables)
        {
            _fileUploadService = fileUploadService;
            _submissionService = submissionService;
            _subsidiaryService = subsidiaryService;
            _basePath = globalVariables.Value.BasePath;
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

            var vm = GetSubsidiaryListViewModel(page);
            if (vm.Organisations.Count == 0)
            {
                return new NotFoundResult();
            }

            return View(vm);
        }

        [HttpPost]
        [Route(PagePaths.FileUploadSubsidiaries)]
        public async Task<IActionResult> Post()
        {
            var submissionId = await _fileUploadService.ProcessUploadAsync(
                Request.ContentType,
                Request.Body,
                "January to June 2024", // TODO , This period needs to be removed for sub.
                ModelState,
                null,
                SubmissionType.Subsidiary,
                null,
                null,
                null);

            var routeValues = new RouteValueDictionary { { "submissionId", submissionId } };

            return !ModelState.IsValid
               ? View("SubsidiariesList", GetSubsidiaryListViewModel(1))
               : RedirectToAction(nameof(FileUploading), routeValues);
        }

        [HttpGet]
        [Route("FileUploading")]
        public async Task<IActionResult> FileUploading()
        {
            var submissionId = Guid.Parse(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<SubsidiarySubmission>(submissionId);
            // TODO - check journey

            if (submission is null)
            {
                return RedirectToAction("Get", "FileUpload");
            }

            var subFileUploadViewModel = new SubFileUploadingViewModel()
            {
                SubmissionId = submissionId.ToString(),
                IsFileUploadTakingLong = submission.SubsidiaryFileUploadDateTime <= DateTime.Now.AddMinutes(-5),
            };
            return submission.SubsidiaryDataComplete || submission.Errors.Any()
                ? RedirectToAction(nameof(FileUplodSuccess), new RouteValueDictionary { { "recordsAdded", submission.RecordsAdded } })
                : View("FileUploading", subFileUploadViewModel);
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiariesSuccess)]
        public async Task<IActionResult> FileUplodSuccess()
        {
            var model = new SubsidiaryFileUplodSuccessViewModel();

            model.RecordsAdded = int.TryParse(Request.Query["recordsAdded"], out var recordsAdded) ? recordsAdded : 0;

            return View("FileUplodSuccess", model);
        }

        [HttpGet]
        [Route(PagePaths.ExportSubsidiaries)]
        public async Task<IActionResult> ExportSubsidiaries(int subsidiaryParentId)
        {
            var userData = User.GetUserData();
            var organisation = userData.Organisations.First();
            bool isComplienceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
            var stream = await _subsidiaryService.GetSubsidiariesStreamAsync(subsidiaryParentId, isComplienceScheme);

            return File(stream, "text/csv", $"subsidiary.csv");
        }

        private static List<SubsidiaryOrganisationViewModel> GetOrganisationAndSubsidiaryList(bool isDirectProducer, string orgName)
        {
            return isDirectProducer ? new List<SubsidiaryOrganisationViewModel>
                {
                    new()
                    {
                        Id = "DirectProducerId",
                        Name = orgName,
                        CompaniesHouseNumber = "AA01234567890",
                        Subsidiaries = new List<SubsidiaryViewModel>
                        {
                            new(123, "Subsidiary One", "SubsidiaryNumber123"),
                            new(456, "Subsidiary Two", "SubsidiaryNumber456"),
                            new(789, "Subsidiary Three", "SubsidiaryNumber789"),
                            new(147, "Subsidiary Four", "SubsidiaryNumber147"),
                            new(258, "Subsidiary Five", "no CH number"),
                        }
                    }
                }
                : new List<SubsidiaryOrganisationViewModel>
                {
                    new()
                    {
                        Id = "ComplianceSchemeOrganisation1",
                        Name = orgName,
                        CompaniesHouseNumber = "AA01234567890",
                        Subsidiaries = new List<SubsidiaryViewModel>
                        {
                            new(123, "ComplianceSchemeOrganisation1 Subsidiary One", "SubsidiaryNumber123"),
                            new(456, "ComplianceSchemeOrganisation1 Subsidiary Two", "SubsidiaryNumber456"),
                            new(789, "ComplianceSchemeOrganisation1 Subsidiary Three", "no CH number"),
                            new(147, "ComplianceSchemeOrganisation1 Subsidiary Four", "SubsidiaryNumber147"),
                            new(258, "ComplianceSchemeOrganisation1 Subsidiary Five", "SubsidiaryNumber258"),
                        }
                    },
                    new()
                    {
                        Id = "ComplianceSchemeOrganisation2",
                        Name = $"{orgName} Org2",
                        CompaniesHouseNumber = "ZZ147258369",
                        Subsidiaries = new List<SubsidiaryViewModel>
                        {
                            new(123, "ComplianceSchemeOrganisation2 Subsidiary One", "SubsidiaryNumber123"),
                            new(456, "ComplianceSchemeOrganisation2 Subsidiary Two", "SubsidiaryNumber456"),
                            new(789, "ComplianceSchemeOrganisation2 Subsidiary Three", "SubsidiaryNumber789"),
                            new(147, "ComplianceSchemeOrganisation2 Subsidiary Four", "no CH number"),
                            new(258, "ComplianceSchemeOrganisation2 Subsidiary Five", "SubsidiaryNumber258"),
                        }
                    }
                };
        }

        private SubsidiaryListViewModel GetSubsidiaryListViewModel(int? page)
        {
            const int showPerPage = 1;

            var userData = User.GetUserData();
            var organisation = userData.Organisations.First();
            var isDirectProducer = organisation.OrganisationRole == OrganisationRoles.Producer;

            ViewBag.BackLinkToDisplay = _basePath;

            var organisations = GetOrganisationAndSubsidiaryList(isDirectProducer, userData.Organisations.First().Name);

            var pageUrl = Url.Action(nameof(SubsidiariesList));

            return new SubsidiaryListViewModel
            {
                Organisations = organisations.Skip(((int)page - 1) * showPerPage)
                    .Take(showPerPage)
                    .ToList(),
                PagingDetail = new PagingDetail
                {
                    CurrentPage = page.Value,
                    PageSize = 1,
                    TotalItems = organisations.Count,
                    PagingLink = $"{pageUrl}?page="
                }
            };
        }
    }
}