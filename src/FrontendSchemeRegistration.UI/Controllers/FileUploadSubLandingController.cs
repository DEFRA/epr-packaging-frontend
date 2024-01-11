namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using ControllerExtensions;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sessions;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadSubLanding)]
public class FileUploadSubLandingController : Controller
{
    private const int _submissionsLimit = 2;
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly List<SubmissionPeriod> _submissionPeriods;
    private readonly string _basePath;

    public FileUploadSubLandingController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _submissionPeriods = globalVariables.Value.SubmissionPeriods;
        _basePath = globalVariables.Value.BasePath;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var periods = _submissionPeriods.Select(x => x.DataPeriod).ToList();
        var submissions = await _submissionService.GetSubmissionsAsync<PomSubmission>(
            periods,
            _submissionsLimit,
            session.RegistrationSession.SelectedComplianceScheme?.Id,
            session.RegistrationSession.IsSelectedComplianceSchemeFirstCreated);
        var submissionPeriodDetails = new List<SubmissionPeriodDetail>();

        foreach (var submissionPeriod in _submissionPeriods)
        {
            var submission = submissions.FirstOrDefault(x => x.SubmissionPeriod == submissionPeriod.DataPeriod);

            var decision = new PomDecision
            {
                IsResubmissionRequired = false,
                Decision = string.Empty,
                Comments = string.Empty
            };

            if (submission != null)
            {
                decision = await _submissionService.GetDecisionAsync<PomDecision>(
                _submissionsLimit,
                submission.Id);
            }

            submissionPeriodDetails.Add(new SubmissionPeriodDetail
            {
                DataPeriod = submissionPeriod.DataPeriod,
                Deadline = submissionPeriod.Deadline,
                Status = GetSubmissionStatus(submission, submissionPeriod, decision),
                IsResubmissionRequired = decision.IsResubmissionRequired,
                Decision = decision.Decision,
                Comments = decision.Comments
            });
        }

        var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;

        if (organisationRole is not null)
        {
            session.RegistrationSession.Journey.ClearReportPackagingDataJourney();
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
            ViewBag.HomeLinkToDisplay = _basePath;

            return View(
                "FileUploadSubLanding",
                new FileUploadSubLandingViewModel
                {
                    SubmissionPeriodDetails = submissionPeriodDetails,
                    ComplianceSchemeName = session.RegistrationSession.SelectedComplianceScheme?.Name,
                    OrganisationRole = organisationRole,
                    ServiceRole = session.UserData?.ServiceRole ?? "Basic User"
                });
        }

        return RedirectToAction("LandingPage", "FrontendSchemeRegistration");
    }

    [HttpPost]
    public async Task<IActionResult> Post(string dataPeriod)
    {
        var selectedSubmissionPeriod = _submissionPeriods.FirstOrDefault(x => x.DataPeriod == dataPeriod);

        if (selectedSubmissionPeriod is null)
        {
            return RedirectToAction(nameof(Get));
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.SubmissionPeriod = selectedSubmissionPeriod.DataPeriod;
        session.RegistrationSession.SubmissionDeadline = selectedSubmissionPeriod.Deadline;

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadSubLanding);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        var submissions = await _submissionService.GetSubmissionsAsync<PomSubmission>(
            new List<string> { selectedSubmissionPeriod.DataPeriod },
            _submissionsLimit,
            session.RegistrationSession.SelectedComplianceScheme?.Id,
            session.RegistrationSession.IsSelectedComplianceSchemeFirstCreated);
        var submission = submissions.FirstOrDefault();

        if (submission is null or { IsSubmitted: false, HasValidFile: false })
        {
            return RedirectToAction(
                nameof(FileUploadController.Get),
                nameof(FileUploadController).RemoveControllerFromName(),
                submission is null ? null : new RouteValueDictionary { { "submissionId", submission.Id } });
        }

        var routeValueDictionary = new RouteValueDictionary { { "submissionId", submission.Id } };

        if (submission is { IsSubmitted: false, HasValidFile: true })
        {
            return RedirectToAction(
                nameof(FileUploadCheckFileAndSubmitController.Get),
                nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName(), routeValueDictionary);
        }

        return submission.LastSubmittedFile.FileId == submission.LastUploadedValidFile.FileId
            ? RedirectToAction(
                nameof(UploadNewFileToSubmitController.Get),
                nameof(UploadNewFileToSubmitController).RemoveControllerFromName(), routeValueDictionary)
            : RedirectToAction(
                nameof(FileUploadCheckFileAndSubmitController.Get),
                nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName(), routeValueDictionary);
    }

    private static SubmissionPeriodStatus GetSubmissionStatus(
        PomSubmission? submission,
        SubmissionPeriod submissionPeriod,
        PomDecision decision)
    {
        if (DateTime.Now < submissionPeriod.ActiveFrom)
        {
            return SubmissionPeriodStatus.CannotStartYet;
        }

        if (submission is null)
        {
            return SubmissionPeriodStatus.NotStarted;
        }

        if (submission.LastSubmittedFile is not null)
        {
            switch (decision.Decision)
            {
                case "Accepted":
                    return SubmissionPeriodStatus.AcceptedByRegulator;
                    break;
                case "Rejected":
                    return SubmissionPeriodStatus.RejectedByRegulator;
                    break;
                case "Approved":
                    return SubmissionPeriodStatus.AcceptedByRegulator;
                    break;
                default:
                    return SubmissionPeriodStatus.SubmittedToRegulator;
                    break;
            }
        }

        return submission is { HasValidFile: true }
            ? SubmissionPeriodStatus.FileUploaded
            : SubmissionPeriodStatus.NotStarted;
    }
}