namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Services.FileUploadLimits;
using Services.Interfaces;
using Services.RegistrationPeriods;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadBrands)]
public class FileUploadBrandsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISessionManager<RegistrationApplicationSession> _registrationApplicationSessionManager;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly IRegistrationPeriodProvider _registrationPeriodProvider;

    public FileUploadBrandsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ISessionManager<RegistrationApplicationSession> registrationApplicationSessionManager,
        IOptions<GlobalVariables> globalVariables,
        IRegistrationPeriodProvider registrationPeriodProvider)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
        _registrationApplicationSessionManager = registrationApplicationSessionManager;
        _globalVariables = globalVariables;
        _registrationPeriodProvider = registrationPeriodProvider;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var registrationYear = _registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadCompanyDetails))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
        if (organisationRole is not null)
        {
            var submissionId = Guid.Parse(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

            if (submission is not null)
            {
                if (submission.Errors.Count > 0)
                {
                    ModelStateHelpers.AddFileUploadExceptionsToModelState(submission.Errors.Distinct().ToList(), ModelState);
                }

                if (submission.RequiresBrandsFile)
                {
                    session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadBrands);
                    await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

                    var registrationApplicationSession = await _registrationApplicationSessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
                    
                    SetBackLinkToOrganisationDetailsUploaded(submissionId, registrationYear, registrationApplicationSession.RegistrationJourney);

                    return View(
                        "FileUploadBrands",
                        new FileUploadSuccessViewModel
                        {
                            OrganisationRole = organisationRole,
                            IsResubmission = session.RegistrationSession.IsResubmission,
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
        Guid? submissionId = Guid.TryParse(Request.Query["submissionId"], out var value) ? value : null;
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
                SubmissionSubType = SubmissionSubType.Brands,
                RegistrationSetId =
                    session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
                IsResubmission = session.RegistrationSession.IsResubmission,
                ComplianceSchemeId = null,
                RegistrationJourney = null
            });

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadBrands);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);
        
        if (!ModelState.IsValid)
        {
            var registrationApplicationSession = await _registrationApplicationSessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
            
            SetBackLinkToOrganisationDetailsUploaded(submissionId, registrationYear, registrationApplicationSession.RegistrationJourney);

            return View("FileUploadBrands", new FileUploadSuccessViewModel
            {
                OrganisationRole = organisationRole,
                IsResubmission = session.RegistrationSession.IsResubmission,
                RegistrationYear = registrationYear,
                RegistrationJourney = registrationApplicationSession.RegistrationJourney
            });
        }
        
        return RedirectToAction(
            "Get",
            "FileUploadingBrands",
            routeValues);
    }
    
    private void SetBackLinkToOrganisationDetailsUploaded(Guid? submissionId, int? registrationYear, RegistrationJourney? registrationJourney)
    {
        if (Url is null) return;

        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear, registrationJourney: registrationJourney);
        var baseUrl = Url.Content($"~{PagePaths.OrganisationDetailsUploaded}");
        ViewBag.BackLinkToDisplay = QueryHelpers.AddQueryString(baseUrl, routeValues.ToDictionary(k => k.Key, k => k.Value?.ToString() ?? string.Empty));
    }
}