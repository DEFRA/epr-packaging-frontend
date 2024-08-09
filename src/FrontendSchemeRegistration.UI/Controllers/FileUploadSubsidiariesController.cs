using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers
{
    [Route(PagePaths.FileUploadSubsidiaries)]
    public class FileUploadSubsidiariesController : Controller
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ISubmissionService _submissionService;
        private readonly ISubsidiaryService _subsidiaryService;

        public FileUploadSubsidiariesController(
            IFileUploadService fileUploadService,
            ISubmissionService submissionService,
            ISubsidiaryService subsidiaryService)
        {
            _fileUploadService = fileUploadService;
            _submissionService = submissionService;
            _subsidiaryService = subsidiaryService;
        }

        [HttpGet]
        [Route(PagePaths.FileUploadSubsidiaries)]
        public IActionResult Index()
        {
            var fileUploadSubsidiaryViewModel = new FileUploadSubsidiaryViewModel { SubsidiariesAvailable = true };
            return View(fileUploadSubsidiaryViewModel);
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
                ? View("Index", new FileUploadSubsidiaryViewModel { SubsidiariesAvailable = true })
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
    }
}
