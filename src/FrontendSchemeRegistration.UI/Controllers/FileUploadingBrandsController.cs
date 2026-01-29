namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.RegistrationPeriods;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadingBrands)]
public class FileUploadingBrandsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISessionManager<RegistrationApplicationSession> _registrationApplicationSessionManager;
    private readonly IRegistrationPeriodProvider _registrationPeriodProvider;

    public FileUploadingBrandsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ISessionManager<RegistrationApplicationSession> registrationApplicationSessionManager,
        IRegistrationPeriodProvider registrationPeriodProvider)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _registrationApplicationSessionManager = registrationApplicationSessionManager;
        _registrationPeriodProvider = registrationPeriodProvider;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var registrationYear = _registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (session is null)
        {
            return await GetFileUploadingBrandsViewResult(submissionId, registrationYear);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadBrands))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        return submission.BrandsDataComplete || submission.Errors.Count > 0
            ? GetNextPage(submission.Id, submission.Errors.Count > 0, registrationYear)
            : await GetFileUploadingBrandsViewResult(submissionId, registrationYear);
    }

    private RedirectToActionResult GetNextPage(Guid submissionId, bool exceptionErrorOccurred, int? registrationYear)
    {
        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);       
        return exceptionErrorOccurred
            ? RedirectToAction("Get", "FileUploadBrands", routeValues)
            : RedirectToAction("Get", "FileUploadBrandsSuccess", routeValues);
    }

    private async Task<ViewResult> GetFileUploadingBrandsViewResult(Guid submissionId, int? registrationYear)
    {
        var registrationApplicationSession = await _registrationApplicationSessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();

        return View("FileUploadingBrands", new FileUploadingViewModel
        {
            SubmissionId = submissionId.ToString(),
            RegistrationYear = registrationYear,
            RegistrationJourney = registrationApplicationSession.RegistrationJourney
        });
    }
}