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

    public FileUploadCompanyDetailsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
        _globalVariables = globalVariables;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
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

                SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission);

                return View(
                    "FileUploadCompanyDetails",
                    new FileUploadCompanyDetailsViewModel
                    {
                        SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                        OrganisationRole = organisationRole,
                        IsResubmission = session.RegistrationSession.IsResubmission
                    });
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
    }    

    [HttpPost]
    [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
    public async Task<IActionResult> Post()
    {
        Guid? submissionId = Guid.TryParse(Request.Query["submissionId"], out var value) ? value : null;
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
        SetBackLink(session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission);

        return !ModelState.IsValid
            ? View(
                "FileUploadCompanyDetails",
                new FileUploadCompanyDetailsViewModel
                {
                    SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                    OrganisationRole = organisationRole,
                    IsResubmission = session.RegistrationSession.IsResubmission
                })
            : RedirectToAction(
                "Get",
                "UploadingOrganisationDetails",
                new RouteValueDictionary
                {
                    {
                        "submissionId", submissionId
                    }
                });
    }

    private void SetBackLink(bool isFileUploadJourneyInvokedViaRegistration, bool isResubmission)
    {
        var backLink = isFileUploadJourneyInvokedViaRegistration ? Url.Content($"~/{PagePaths.RegistrationTaskList}") : Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");
        ViewBag.BackLinkToDisplay = backLink.AppendResubmissionFlagToQueryString(isResubmission);
    }
}