namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadPartnershipsSuccess)]
public class FileUploadPartnershipsSuccessController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public FileUploadPartnershipsSuccessController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _registrationApplicationService = registrationApplicationService;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var registrationYear = _registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails", registrationYear is not null ? new { registrationyear = registrationYear.ToString() } : null);
        }

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadPartnerships))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }
        
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        return View("FileUploadPartnershipsSuccess", new FileUploadSuccessViewModel
        {
            FileName = submission.PartnershipsFileName,
            SubmissionId = submissionId,
            IsResubmission = session.RegistrationSession.IsResubmission,
            RegistrationYear = registrationYear,
            OrganisationName = organisation.Name
        });
    }
}