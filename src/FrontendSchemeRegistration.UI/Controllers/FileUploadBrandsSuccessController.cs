namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadBrandsSuccess)]
public class FileUploadBrandsSuccessController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadBrandsSuccessController(ISubmissionService submissionService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetails)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is not null)
        {
            if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadBrands))
            {
                return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
            }

            var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
            if (organisationRole is not null)
            {
                var submissionId = Guid.Parse(Request.Query["submissionId"]);
                var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);
                if (submission is { RequiresBrandsFile: true, BrandsDataComplete: true })
                {
                    return View("FileUploadBrandsSuccess", new FileUploadBrandsSuccessViewModel
                    {
                        SubmissionId = submission.Id,
                        FileName = submission.BrandsFileName,
                        RequiresPartnershipsFile = submission.RequiresPartnershipsFile,
                        OrganisationRole = organisationRole,
                        IsApprovedUser = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
                        IsResubmission = session.RegistrationSession.IsResubmission
                    });
                }
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetails");
    }
}
