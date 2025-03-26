namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadingPartnerships)]
public class FileUploadingPartnershipsController(ISubmissionService submissionService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager) : Controller
{
    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        if (session is null)
        {
            return GetFileUploadingPartnershipsViewResult(submissionId);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadPartnerships))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        return submission.PartnershipsDataComplete || submission.Errors.Count > 0
            ? GetNextPage(submission.Id, submission.Errors.Count > 0)
            : GetFileUploadingPartnershipsViewResult(submissionId);
    }

    private RedirectToActionResult GetNextPage(Guid submissionId, bool exceptionErrorOccurred)
    {
        var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

        return exceptionErrorOccurred
            ? RedirectToAction("Get", "FileUploadPartnerships", routeValues)
            : RedirectToAction("Get", "FileUploadPartnershipsSuccess", routeValues);
    }

    private ViewResult GetFileUploadingPartnershipsViewResult(Guid submissionId)
    {
        return View("FileUploadingPartnerships", new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }
}