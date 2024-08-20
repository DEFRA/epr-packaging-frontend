namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public FileUploadBrandsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetails");
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
                            OrganisationRole = organisationRole
                        });
                }
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetails");
    }

    [HttpPost]
    [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    [SubmissionPeriodActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Post()
    {
        Guid? submissionId = Guid.TryParse(Request.Query["submissionId"], out var value) ? value : null;
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            session.RegistrationSession.SubmissionPeriod,
            ModelState,
            submissionId,
            SubmissionType.Registration,
            SubmissionSubType.Brands,
            session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
            null);

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadBrands);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return !ModelState.IsValid
            ? View("FileUploadBrands", new FileUploadSuccessViewModel
            {
                OrganisationRole = organisationRole
            })
            : RedirectToAction(
                "Get",
                "FileUploadingBrands",
                new RouteValueDictionary
                {
                    {
                        "submissionId", submissionId
                    }
                });
    }
}