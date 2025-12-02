namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.Application.Options;
using global::FrontendSchemeRegistration.UI.Services;
using global::FrontendSchemeRegistration.UI.Services.FileUploadLimits;
using global::FrontendSchemeRegistration.UI.Services.Messages;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadCompanyDetails)]
[SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
[ComplianceSchemeIdActionFilter]
public class FileUploadCompanyDetailsController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISubmissionService _submissionService;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public FileUploadCompanyDetailsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
        _globalVariables = globalVariables;
        _registrationApplicationService = registrationApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]string? registrationYear = null, [FromQuery]ProducerSize? producerSize = null)
    {
        var validatedRegistrationYear = _registrationApplicationService.ValidateRegistrationYear(registrationYear, true);
        
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is not null)
        {
            var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;

            if (organisationRole is not null)
            {
                if (Guid.TryParse(Request.Query["SubmissionId"], out var submissionId))
                {
                    var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
                    if (submission != null && submission.Errors.Count > 0)
                    {
                        ModelStateHelpers.AddFileUploadExceptionsToModelState(submission.Errors.Distinct().ToList(), ModelState);
                    }
                }

                this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, validatedRegistrationYear, producerSize:producerSize);

                var viewName = producerSize == null ? "FileUploadCompanyDetails" : "FileUploadCompanyDetailsCso";
                return View(
                    viewName,
                    new FileUploadCompanyDetailsViewModel
                    {
                        SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                        OrganisationRole = organisationRole,
                        IsResubmission = session.RegistrationSession.IsResubmission,
                        RegistrationYear = validatedRegistrationYear,
                        ProducerSize = producerSize
                    });
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
    }    

    [HttpPost]
    [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
    public async Task<IActionResult> Post(string? registrationyear, ProducerSize? producerSize)
    {
        Guid? submissionId = Guid.TryParse(Request.Query["submissionId"], out var value) ? value : null;
        var registrationYear =  _registrationApplicationService.ValidateRegistrationYear(registrationyear, true);

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.LatestRegistrationSet ??= new Dictionary<string, Guid>();

        session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod] =
            Guid.NewGuid();

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            session.RegistrationSession.SubmissionPeriod,
            ModelState,
            submissionId,
            SubmissionType.Registration,
            new DefaultFileUploadMessages(),
            new DefaultFileUploadLimit(_globalVariables),
            SubmissionSubType.CompanyDetails,
            session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
            session.RegistrationSession.SelectedComplianceScheme?.Id,
            session.RegistrationSession.IsResubmission);

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadCompanyDetails);
        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        this.SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, registrationYear);
        var routeValue = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear, producerSize: producerSize);

        var viewName = producerSize == null ? "FileUploadCompanyDetails" : "FileUploadCompanyDetailsCso";
        return !ModelState.IsValid
            ? View(
                viewName,
                new FileUploadCompanyDetailsViewModel
                {
                    SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                    OrganisationRole = organisationRole,
                    IsResubmission = session.RegistrationSession.IsResubmission,
                    RegistrationYear = registrationYear,
                    ProducerSize = producerSize
                })
            : RedirectToAction(
                "Get",
                "UploadingOrganisationDetails",
                routeValue);
    }

}