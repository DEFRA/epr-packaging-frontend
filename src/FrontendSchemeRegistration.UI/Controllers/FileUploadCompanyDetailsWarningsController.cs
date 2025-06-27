using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadCompanyDetailsWarnings)]
[SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
[ComplianceSchemeIdActionFilter]
public class FileUploadCompanyDetailsWarningsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ValidationOptions _validationOptions;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public FileUploadCompanyDetailsWarningsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<ValidationOptions> validationOptions,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _validationOptions = validationOptions.Value;
        _registrationApplicationService = registrationApplicationService;

    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var registrationYear = _registrationApplicationService.validateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null || !submission.CompanyDetailsDataComplete)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (session.RegistrationSession.Journey.Count == 0 || !session.RegistrationSession.Journey.Contains(PagePaths.FileUploadCompanyDetails))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, registrationYear);

        return View(
           "FileUploadCompanyDetailsWarnings",
           new FileUploadWarningViewModel
           {
               FileName = submission.CompanyDetailsFileName,
               SubmissionId = submissionId,
               MaxWarningsToProcess = _validationOptions.MaxIssuesToProcess,
               MaxReportSize = _validationOptions.MaxIssueReportSize,
               RegistrationYear = registrationYear
           });
    }

    [HttpPost]
    public async Task<IActionResult> FileUploadDecision(FileUploadWarningViewModel model)
    {
        ModelState.Remove(nameof(model.FileName));
        ModelState.Remove(nameof(model.MaxReportSize));

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, model.RegistrationYear);

        if (!ModelState.IsValid)
        {
            return View("FileUploadCompanyDetailsWarnings", model);
        }

        if (model.UploadNewFile.HasValue)
        {
            return model.UploadNewFile.Value ?
                RedirectToAction("Get", "FileUploadCompanyDetails", model.RegistrationYear.HasValue ? new { submissionId = model.SubmissionId,  registrationyear = model.RegistrationYear } : new { submissionId = model.SubmissionId }) :
                RedirectToAction("Get", "ReviewCompanyDetails", model.RegistrationYear.HasValue ? new { submissionId = model.SubmissionId, registrationyear = model.RegistrationYear } : new { submissionId = model.SubmissionId });
        }

        return View("FileUploadCompanyDetailsWarnings", model);
    }

}
