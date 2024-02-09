namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.OrganisationDetailsUploaded)]
public class FileUploadCompanyDetailsSuccessController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public FileUploadCompanyDetailsSuccessController(ISubmissionService submissionService, ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.OrganisationDetailsUploaded)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is not null)
        {
            if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadCompanyDetails))
            {
                return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
            }

            var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;

            if (organisationRole is not null)
            {
                var submissionId = Guid.Parse(Request.Query["submissionId"]);
                var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

                if (submission != null && !submission.RequiresBrandsFile && submission.RequiresPartnershipsFile)
                {
                    session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadBrands);
                    await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
                }

                if (submission is not null)
                {
                    return View(
                        "FileUploadCompanyDetailsSuccess",
                        new FileUploadCompanyDetailsSuccessViewModel
                        {
                            SubmissionId = submission.Id,
                            FileName = submission.CompanyDetailsFileName,
                            RequiresBrandsFile = submission.RequiresBrandsFile,
                            RequiresPartnershipsFile = submission.RequiresPartnershipsFile,
                            SubmissionDeadline = session.RegistrationSession.SubmissionDeadline,
                            OrganisationRole = organisationRole,
                            IsApprovedUser = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
                            OrganisationMemberCount = organisationRole == OrganisationRoles.ComplianceScheme ? submission.OrganisationMemberCount : null
                        });
                }
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetails");
    }
}