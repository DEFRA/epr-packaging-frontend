using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
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
    private readonly IRegistrationPeriodProvider _registrationPeriodProvider;

    public FileUploadCompanyDetailsWarningsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<ValidationOptions> validationOptions,
        IRegistrationPeriodProvider registrationPeriodProvider)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _validationOptions = validationOptions.Value;
        _registrationPeriodProvider = registrationPeriodProvider;

    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var registrationYear = _registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);
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

        var organisation = session.UserData.Organisations[0];
        var organisationRole = organisation.OrganisationRole;
        bool isCso = organisationRole == OrganisationRoles.ComplianceScheme;
        var regJourney = submission.RegistrationJourney ?? registrationJourney;

        this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, registrationYear, regJourney);

        return View(
           "FileUploadCompanyDetailsWarnings",
           new FileUploadWarningViewModel
           {
               FileName = submission.CompanyDetailsFileName,
               SubmissionId = submissionId,
               MaxWarningsToProcess = _validationOptions.MaxIssuesToProcess,
               MaxReportSize = _validationOptions.MaxIssueReportSize,
               RegistrationYear = registrationYear,
               RegistrationJourney = regJourney,
               IsCso = isCso,
               OrganisationName = organisation.Name
           });
    }

    [HttpPost]
    public async Task<IActionResult> FileUploadDecision(FileUploadWarningViewModel model)
    {
        ModelState.Remove(nameof(model.FileName));
        ModelState.Remove(nameof(model.MaxReportSize));
        ModelState.Remove(nameof(model.OrganisationName));
        ModelState.Remove(nameof(model.IsCso));

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var regJourney = model.RegistrationJourney;
        this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, model.RegistrationYear, regJourney);

        if (!ModelState.IsValid)
        {
            return View("FileUploadCompanyDetailsWarnings", model);
        }

        if (model.UploadNewFile == true)
        {
            var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: model.SubmissionId, registrationYear: model.RegistrationYear, registrationJourney: model.RegistrationJourney);
            return RedirectToAction("Get", "FileUploadCompanyDetails", routeValues);
        }

        var successRouteValues = QueryStringExtensions.BuildRouteValues(submissionId: model.SubmissionId, registrationYear: model.RegistrationYear, registrationJourney: model.RegistrationJourney);
        return RedirectToAction("Get", "FileUploadCompanyDetailsSuccess", successRouteValues);
    }
}
