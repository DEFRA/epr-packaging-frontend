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
[Route(PagePaths.FileUploadPartnerships)]
public class FileUploadPartnershipsController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISubmissionService _submissionService;
    private IOptions<GlobalVariables> _globalVariables;


    public FileUploadPartnershipsController(
        ISubmissionService submissionService,
        IFileUploadService fileUploadService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables)
    {
        _submissionService = submissionService;
        _fileUploadService = fileUploadService;
        _sessionManager = sessionManager;
        _globalVariables = globalVariables;
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

        if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadBrands))
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
        if (organisationRole is not null)
        {
            var submissionId = new Guid(Request.Query["submissionId"]);
            var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

            if (submission is not null)
            {
                if (submission.Errors.Count > 0)
                {
                    ModelStateHelpers.AddFileUploadExceptionsToModelState(submission.Errors.Distinct().ToList(), ModelState);
                }

                if (submission.RequiresPartnershipsFile)
                {
                    return View(
                        "FileUploadPartnerships",
                        new FileUploadViewModel
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
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            session.RegistrationSession.SubmissionPeriod,
            ModelState,
            submissionId,
            SubmissionType.Registration,
            new DefaultFileUploadMessages(),
            new DefaultFileUploadLimit(_globalVariables),
            SubmissionSubType.Partnerships,
            session.RegistrationSession.LatestRegistrationSet[session.RegistrationSession.SubmissionPeriod],
            null);

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadPartnerships);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return !ModelState.IsValid
            ? View("FileUploadPartnerships", new FileUploadViewModel
            {
                OrganisationRole = organisationRole
            })
            : RedirectToAction(
                "Get",
                "FileUploadingPartnerships",
                new RouteValueDictionary
                {
                    {
                        "submissionId", submissionId
                    }
                });
    }
}