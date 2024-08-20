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
[Route(PagePaths.FileUploadingBrands)]
public class FileUploadingBrandsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadingBrandsController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadBrands))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        return submission.BrandsDataComplete || submission.Errors.Count > 0
            ? GetNextPage(submission.Id, submission.Errors.Count > 0)
            : View("FileUploadingBrands", new FileUploadingViewModel { SubmissionId = submissionId.ToString() });
    }

    private RedirectToActionResult GetNextPage(Guid submissionId, bool exceptionErrorOccurred)
    {
        var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };

        return exceptionErrorOccurred
            ? RedirectToAction("Get", "FileUploadBrands", routeValues)
            : RedirectToAction("Get", "FileUploadBrandsSuccess", routeValues);
    }
}