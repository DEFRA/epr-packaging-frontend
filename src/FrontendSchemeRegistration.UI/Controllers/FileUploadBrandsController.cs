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
[Route(PagePaths.FileUploadBrands)]
public class FileUploadBrandsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IOptions<GlobalVariables> _globalVariables;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public FileUploadBrandsController(
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
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var registrationYear = _registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

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

                    return View(
                        "FileUploadBrands",
                        new FileUploadSuccessViewModel
                        {
                            OrganisationRole = organisationRole,
                            IsResubmission = session.RegistrationSession.IsResubmission,
                            RegistrationYear = registrationYear
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
        var registrationYear =  _registrationApplicationService.ValidateRegistrationYear(registrationyear, true);

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            session.RegistrationSession.SubmissionPeriod,
        ModelState,
        submissionId,
            SubmissionType.Registration,
            new DefaultFileUploadMessages(),
            new DefaultFileUploadLimit(_globalVariables),
            SubmissionSubType.Brands,
            session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
            null,
            session.RegistrationSession.IsResubmission);

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadBrands);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);        
        return !ModelState.IsValid
            ? View("FileUploadBrands", new FileUploadSuccessViewModel
            {
                OrganisationRole = organisationRole,
                IsResubmission = session.RegistrationSession.IsResubmission,
                RegistrationYear = registrationYear
            })
            : RedirectToAction(
                "Get",
                "FileUploadingBrands",
                routeValues);
    }
}