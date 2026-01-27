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
using global::FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadPartnerships)]
public class FileUploadPartnershipsController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISessionManager<RegistrationApplicationSession> _registrationApplicationSession;
    private readonly ISubmissionService _submissionService;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly IRegistrationPeriodProvider _registrationPeriodProvider;

    public FileUploadPartnershipsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegistrationPeriodProvider registrationPeriodProvider,
        IOptions<GlobalVariables> globalVariables,
        ISessionManager<RegistrationApplicationSession> registrationApplicationSession)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
        _registrationPeriodProvider = registrationPeriodProvider;
        _globalVariables = globalVariables;
        _registrationApplicationSession = registrationApplicationSession;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var registrationYear = _registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);
        
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }
        
        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadBrands))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }
        
        var registrationApplicationSession = await _registrationApplicationSession.GetSessionAsync(HttpContext.Session);

        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
        if (organisationRole is not null)
        {
            var submissionId = new Guid(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

            if (submission is not null)
            {
                if (submission.Errors.Count > 0)
                {
                    ModelStateHelpers.AddFileUploadExceptionsToModelState(submission.Errors.Distinct().ToList(), ModelState);
                }
                
                ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadBrandsSuccess}")
                    .AppendBackLink(session.RegistrationSession.IsResubmission, registrationYear, registrationJourney:submission.RegistrationJourney, submissionId: submissionId);
                
                if (submission.RequiresPartnershipsFile)
                {
                    return View(
                        "FileUploadPartnerships",
                        new FileUploadViewModel
                        {
                            OrganisationRole = organisationRole,
                            RegistrationYear = registrationYear,
                            RegistrationJourney = registrationApplicationSession.RegistrationJourney
                        });
                }
            }
        }
        return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
    }

    [HttpPost]
    [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Post(string? registrationyear)
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
        var registrationYear = _registrationPeriodProvider.ValidateRegistrationYear(registrationyear, true);

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            ModelState,
            new DefaultFileUploadLimit(_globalVariables),
            new FileUploadSubmissionDetails()
            {
                SubmissionPeriod = session.RegistrationSession.SubmissionPeriod,
                SubmissionId = submissionId,
                SubmissionType = SubmissionType.Registration,
                SubmissionSubType = SubmissionSubType.Partnerships,
                RegistrationSetId =
                    session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
                IsResubmission = session.RegistrationSession.IsResubmission,
                ComplianceSchemeId = null,
                RegistrationJourney = null
            });
            
        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadPartnerships);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);
        
        if (!ModelState.IsValid)
        {
            var registrationApplicationSession = await _registrationApplicationSession.GetSessionAsync(HttpContext.Session);
            return View("FileUploadPartnerships", new FileUploadViewModel
            {
                OrganisationRole = organisationRole,
                RegistrationYear = registrationYear,
                RegistrationJourney = registrationApplicationSession.RegistrationJourney
            });    
        }
        
        return RedirectToAction(
            "Get",
            "FileUploadingPartnerships",
            routeValues);
    }
}