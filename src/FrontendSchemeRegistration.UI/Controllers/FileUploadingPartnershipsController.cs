namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using global::FrontendSchemeRegistration.UI.Extensions;
using global::FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadingPartnerships)]
public class FileUploadingPartnershipsController(ISubmissionService submissionService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager, IRegistrationApplicationService registrationApplicationService) : Controller
{
    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var registrationYear = registrationApplicationService.validateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (session is null)
        {
            return GetFileUploadingPartnershipsViewResult(submissionId, registrationYear);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadPartnerships))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        return submission.PartnershipsDataComplete || submission.Errors.Count > 0
            ? GetNextPage(submission.Id, submission.Errors.Count > 0, registrationYear)
            : GetFileUploadingPartnershipsViewResult(submissionId, registrationYear);
    }

    private RedirectToActionResult GetNextPage(Guid submissionId, bool exceptionErrorOccurred, int? registrationYear)
    {
        var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);        
        return exceptionErrorOccurred
            ? RedirectToAction("Get", "FileUploadPartnerships", routeValues)
            : RedirectToAction("Get", "FileUploadPartnershipsSuccess", routeValues);
    }

    private ViewResult GetFileUploadingPartnershipsViewResult(Guid submissionId, int? registrationYear)
    {
        return View("FileUploadingPartnerships", registrationYear is not null ? new FileUploadingViewModel { SubmissionId = submissionId.ToString(), RegistrationYear = registrationYear } : new FileUploadingViewModel { SubmissionId = submissionId.ToString() });

    }
}