namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.FileUploadLimits;
using Services.Interfaces;
using Services.Messages;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUpload)]
[SubmissionPeriodActionFilter(PagePaths.FileUploadSubLanding)]
[ComplianceSchemeIdActionFilter]
public class FileUploadController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ISubmissionService _submissionService;
    private readonly IOptions<GlobalVariables> _globalVariables;

    public FileUploadController(
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
    public async Task<IActionResult> Get()
    {
        if (Guid.TryParse(Request.Query["submissionId"], out var submissionId))
        {
            var submission = await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

            if (submission != null && submission.Errors.Count > 0 && bool.TryParse(Request.Query["showErrors"], out var showErrors) && showErrors)
            {
                ModelStateHelpers.AddFileUploadExceptionsToModelState(submission.Errors.Distinct().ToList(), ModelState);
            }
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session is not null)
        {
            if (!session.RegistrationSession.Journey.Contains<string>(PagePaths.FileUploadSubLanding))
            {
                return RedirectToAction("Get", "FileUploadSubLanding");
            }

            var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;
            if (organisationRole is not null)
            {
                return View("FileUpload", new FileUploadViewModel
                {
                    OrganisationRole = organisationRole
                });
            }
        }

        return RedirectToAction("Get", "FileUploadSubLanding");
    }

    [HttpPost]
    [RequestSizeLimit(FileSizeLimit.FileSizeLimitInBytes)]
    public async Task<IActionResult> Post()
    {
        Guid? submissionId = Guid.TryParse(Request.Query["submissionId"], out var value) ? value : null;
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session?.UserData.Organisations.FirstOrDefault()?.OrganisationRole == null)
        {
            return RedirectToPage(PagePaths.FileUploadSubLanding);
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUpload);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        submissionId = await _fileUploadService.ProcessUploadAsync(
            Request.ContentType,
            Request.Body,
            session.RegistrationSession.SubmissionPeriod,
            ModelState,
            submissionId,
            SubmissionType.Producer, 
            new DefaultFileUploadMessages(), 
            new DefaultFileUploadLimit(_globalVariables),
            null,
            null,
            session.RegistrationSession.SelectedComplianceScheme?.Id);

        var routeValues = new RouteValueDictionary { { "submissionId", submissionId } };

        return !ModelState.IsValid
            ? View("FileUpload", new FileUploadViewModel
            {
                OrganisationRole = session.UserData.Organisations[0].OrganisationRole
            })
            : RedirectToAction("Get", "FileUploading", routeValues);
    }
}