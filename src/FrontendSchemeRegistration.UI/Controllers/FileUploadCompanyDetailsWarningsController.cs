using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
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

    public FileUploadCompanyDetailsWarningsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<ValidationOptions> validationOptions)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _validationOptions = validationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null || !submission.CompanyDetailsDataComplete)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        if (session.RegistrationSession.Journey.Count == 0 || !session.RegistrationSession.Journey.Contains(PagePaths.FileUploadCompanyDetails))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission);

        return View(
           "FileUploadCompanyDetailsWarnings",
           new FileUploadWarningViewModel
           {
               FileName = submission.CompanyDetailsFileName,
               SubmissionId = submissionId,
               MaxWarningsToProcess = _validationOptions.MaxIssuesToProcess,
               MaxReportSize = _validationOptions.MaxIssueReportSize
           });
    }

    [HttpPost]
    public async Task<IActionResult> FileUploadDecision(FileUploadWarningViewModel model)
    {
        ModelState.Remove(nameof(model.FileName));
        ModelState.Remove(nameof(model.MaxReportSize));

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission);

        if (!ModelState.IsValid)
        {
            return View("FileUploadCompanyDetailsWarnings", model);
        }

        if (model.UploadNewFile.HasValue)
        {
            return model.UploadNewFile.Value ?
                RedirectToAction("Get", "FileUploadCompanyDetails", new { submissionId = model.SubmissionId }) :
                RedirectToAction("Get", "ReviewCompanyDetails", new { submissionId = model.SubmissionId });
        }

        return View("FileUploadCompanyDetailsWarnings", model);
    }

    private void SetBackLink(bool isFileUploadJourneyInvokedViaRegistration, bool isResubmission)
    {
        var backLink = isFileUploadJourneyInvokedViaRegistration ? $"/report-data/{PagePaths.RegistrationTaskList}" : Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");
        ViewBag.BackLinkToDisplay = backLink.AppendResubmissionFlagToQueryString(isResubmission);
    }
}
