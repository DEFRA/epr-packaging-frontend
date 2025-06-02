using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Resources.Views.FileUpload;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.FileUploadSubLanding)]
public class FileUploadSubLandingController(
    ISubmissionService submissionService,
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IFeatureManager featureManager,
    IOptions<GlobalVariables> globalVariables,
    IResubmissionApplicationService resubmissionApplicationService)
    : Controller
{
    private const int SubmissionsLimit = 1;
    private List<SubmissionPeriod> _submissionPeriods = globalVariables.Value.SubmissionPeriods;
    private readonly string _basePath = globalVariables.Value.BasePath;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var showPomDecision = await featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowPoMResubmission));

        var showPomSubmissions2025 = await featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowPoMSubmission2025));
        _submissionPeriods = _submissionPeriods.Where(p => showPomSubmissions2025 || p.Year != "2025").ToList();
        var periods = _submissionPeriods.Select(x => x.DataPeriod).ToList();

        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var submissions = await submissionService.GetSubmissionsAsync<PomSubmission>(
            periods,
            periods.Count,
            session.RegistrationSession.SelectedComplianceScheme?.Id);
        var submissionPeriodDetails = new List<SubmissionPeriodDetail>();

        var packagingResubmissionApplicationSessionForAllSubmissionPeriods = await resubmissionApplicationService.GetPackagingResubmissionApplicationSession(
            session.UserData.Organisations[0],
            periods,
            session.RegistrationSession.SelectedComplianceScheme?.Id);

        session.PomResubmissionSession.PackagingResubmissionApplicationSessions = packagingResubmissionApplicationSessionForAllSubmissionPeriods;

        foreach (var submissionPeriod in _submissionPeriods)
        {
            var submission = submissions.Find(x => x.SubmissionPeriod == submissionPeriod.DataPeriod);

            var packagingResubmissionApplicationSession = packagingResubmissionApplicationSessionForAllSubmissionPeriods.Find(x => x.SubmissionId == submission?.Id);

            var decision = new PomDecision();

            if (showPomDecision && submission != null)
            {
                decision = await submissionService.GetDecisionAsync<PomDecision>(
                SubmissionsLimit,
                submission.Id,
                SubmissionType.Producer);

                decision ??= new PomDecision();
            }

            var submissionPeriodDetail = new SubmissionPeriodDetail
            {
                DataPeriod = submissionPeriod.DataPeriod,
                DatePeriodStartMonth = submissionPeriod.LocalisedMonth(MonthType.Start),
                DatePeriodEndMonth = submissionPeriod.LocalisedMonth(MonthType.End),
                DatePeriodYear = submissionPeriod.Year,
                Deadline = submissionPeriod.Deadline,
                Status = GetSubmissionStatus(submission, submissionPeriod, decision, showPomDecision, packagingResubmissionApplicationSession),
                IsResubmissionRequired = decision.IsResubmissionRequired,
                Decision = decision.Decision,
                Comments = decision.Comments,
                IsSubmitted = submission?.IsSubmitted ?? false,
                IsResubmissionComplete = packagingResubmissionApplicationSession != null ? packagingResubmissionApplicationSession.IsResubmissionComplete : null,
            };

            UpdateInProgressSubmissionPeriodStatus(submissionPeriodDetail, packagingResubmissionApplicationSession);
            submissionPeriodDetails.Add(submissionPeriodDetail);
        }

        var submissionPeriodDetailGroups = submissionPeriodDetails
                              .OrderByDescending(c => c.DatePeriodYear)
                              .GroupBy(c => new { c.DatePeriodYear })
                              .Select(c => new SubmissionPeriodDetailGroup
                              {
                                  DatePeriodYear = c.Key.DatePeriodYear,
                                  SubmissionPeriodDetails = c.ToList(),
                                  Quantity = c.Count()
                              }).ToList();

        var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;
        if (organisationRole is not null)
        {
            session.RegistrationSession.Journey.ClearReportPackagingDataJourney();
            await sessionManager.SaveSessionAsync(HttpContext.Session, session);
            ViewBag.HomeLinkToDisplay = _basePath;

            return View(
                "FileUploadSubLanding",
                new FileUploadSubLandingViewModel
                {
                    SubmissionPeriodDetailGroups = submissionPeriodDetailGroups,
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
        var selectedSubmissionPeriod = FindSubmissionPeriod(dataPeriod);
        if (selectedSubmissionPeriod == null)
        {
            return RedirectToAction(nameof(Get));
        }

        await UpdateSessionForSelectedPeriodAsync(selectedSubmissionPeriod);

        var (submissions, submission) = await FindSubmissionForPeriodAsync(selectedSubmissionPeriod.DataPeriod);

        if (submission == null)
        {
            return RedirectToFileUploadController();
        }

        return await HandleSubmissionBasedOnStatus(submission, submissions, selectedSubmissionPeriod.DataPeriod);
    }

    private static void UpdateInProgressSubmissionPeriodStatus(
        SubmissionPeriodDetail submissionPeriodDetail,
        PackagingResubmissionApplicationSession packagingResubmissionApplicationSession)
    {
        if (submissionPeriodDetail.Status == SubmissionPeriodStatus.InProgress)
        {
            submissionPeriodDetail.InProgressSubmissionPeriodStatus = packagingResubmissionApplicationSession == null
                ? null
                : packagingResubmissionApplicationSession.ApplicationInProgressSubmissionPeriodStatus;
        }
    }

    private SubmissionPeriodStatus GetSubmissionStatus(
        PomSubmission? submission,
        SubmissionPeriod submissionPeriod,
        PomDecision decision,
        bool showPomDecision,
        PackagingResubmissionApplicationSession session)
    {
        if (DateTime.Now < submissionPeriod.ActiveFrom)
        {
            return SubmissionPeriodStatus.CannotStartYet;
        }

        if (submission is null)
        {
            return SubmissionPeriodStatus.NotStarted;
        }

        if (featureManager.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)).Result && session?.IsResubmissionInProgress == true)
        {
            return SubmissionPeriodStatus.InProgress;
        }

        if (submission.LastSubmittedFile is not null)
        {
            if (!showPomDecision)
            {
                return SubmissionPeriodStatus.SubmittedToRegulator;
            }

            switch (decision.Decision)
            {
                case "Accepted":
                case "Approved":
                    return SubmissionPeriodStatus.AcceptedByRegulator;
                    break;
                case "Rejected":
                    return SubmissionPeriodStatus.RejectedByRegulator;
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

    private SubmissionPeriod FindSubmissionPeriod(string dataPeriod)
    {
        return _submissionPeriods.Find(period => period.DataPeriod == dataPeriod);
    }

    private async Task UpdateSessionForSelectedPeriodAsync(SubmissionPeriod selectedSubmissionPeriod)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        session.PomResubmissionSession.IsPomResubmissionJourney = false;
        session.RegistrationSession.SubmissionPeriod = selectedSubmissionPeriod.DataPeriod;
        session.RegistrationSession.SubmissionDeadline = selectedSubmissionPeriod.Deadline;
        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadSubLanding);
        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private async Task UpdateSessionForResubmissionJourney(List<PomSubmission> pomSubmissions, string submissionPeriod)
    {
        if (pomSubmissions != null && pomSubmissions.Count > 0)
        {
            var session = await sessionManager.GetSessionAsync(HttpContext.Session);
            session.PomResubmissionSession.PomSubmissions = pomSubmissions;
            session.PomResubmissionSession.SubmissionPeriod = submissionPeriod;
            session.PomResubmissionSession.PomSubmission = pomSubmissions.Find(x => x.SubmissionPeriod == submissionPeriod);
            await sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }
    }

    private async Task<(List<PomSubmission>, PomSubmission)> FindSubmissionForPeriodAsync(string dataPeriod)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var submissions = await submissionService.GetSubmissionsAsync<PomSubmission>(
            new List<string> { dataPeriod },
            _submissionPeriods.Count,
            session.RegistrationSession.SelectedComplianceScheme?.Id);

        return (submissions, submissions.FirstOrDefault());
    }

    private RedirectToActionResult RedirectToFileUploadController()
    {
        return RedirectToAction(
            nameof(FileUploadController.Get),
            nameof(FileUploadController).RemoveControllerFromName());
    }

    private async Task<RedirectToActionResult> HandleSubmissionBasedOnStatus(PomSubmission submission, List<PomSubmission> submissions, string submissionPeriod)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        var organisationId = session.UserData.Organisations.FirstOrDefault()?.Id;
        if (organisationId is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var routeValueDictionary = new RouteValueDictionary { { "submissionId", submission.Id } };

        await UpdateSessionForResubmissionJourney(submissions, submissionPeriod);

        if (!submission.IsSubmitted)
        {
            return HandleUnsubmittedSubmission(submission);
        }

        if (!await featureManager.IsEnabledAsync(nameof(FeatureFlags.ImplementPackagingDataResubmissionJourney)))
        {
            return HandleSubmittedSubmission(submission);
        }

        var isAnySubmissionAcceptedForDataPeriod = await submissionService.IsAnySubmissionAcceptedForDataPeriod(submission, organisationId.Value, session.RegistrationSession.SelectedComplianceScheme?.Id);

        if (!isAnySubmissionAcceptedForDataPeriod)
        {
            return RedirectToAction(
                nameof(FileUploadController.Get),
                nameof(FileUploadController).RemoveControllerFromName(),
                routeValueDictionary);
        }

        var packagingResubmissionApplicationSession = session.PomResubmissionSession.PackagingResubmissionApplicationSessions.Find(x => x.SubmissionId == submission.Id);

        if (packagingResubmissionApplicationSession.IsResubmissionInProgress || packagingResubmissionApplicationSession.IsResubmissionComplete)
        {
            return RedirectToAction(
               nameof(PackagingDataResubmissionController.ResubmissionTaskList),
               nameof(PackagingDataResubmissionController).RemoveControllerFromName());
        }

        return HandleSubmittedSubmission(submission);
    }

    private RedirectToActionResult HandleSubmittedSubmission(PomSubmission submission)
    {
        var routeValueDictionary = new RouteValueDictionary { { "submissionId", submission.Id } };

        if (SubmissionFileIdsDiffer(submission))
        {
            return RedirectToWarningController(routeValueDictionary);
        }

        return RedirectToAppropriateFileController(submission, routeValueDictionary);
    }
    private static bool SubmissionFileIdsDiffer(PomSubmission submission)
    {
        return submission.LastSubmittedFile.FileId != submission.LastUploadedValidFile.FileId &&
               submission.HasWarnings && submission.ValidationPass;
    }

    private RedirectToActionResult RedirectToAppropriateFileController(PomSubmission submission, RouteValueDictionary routeValueDictionary)
    {
        return submission.LastSubmittedFile.FileId == submission.LastUploadedValidFile.FileId
            ? RedirectToAction(
                nameof(UploadNewFileToSubmitController.Get),
                nameof(UploadNewFileToSubmitController).RemoveControllerFromName(),
                routeValueDictionary)
            : RedirectToAction(
                nameof(FileUploadCheckFileAndSubmitController.Get),
                nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName(),
                routeValueDictionary);
    }

    private RedirectToActionResult HandleUnsubmittedSubmission(PomSubmission submission)
    {
        var routeValueDictionary = new RouteValueDictionary { { "submissionId", submission.Id } };

        if (submission.HasWarnings && submission.ValidationPass)
        {
            return RedirectToWarningController(routeValueDictionary);
        }

        if (submission.HasValidFile)
        {
            return RedirectToCheckFileAndSubmitController(routeValueDictionary);
        }

        return RedirectToAction(
            nameof(FileUploadController.Get),
            nameof(FileUploadController).RemoveControllerFromName(),
            routeValueDictionary);
    }

    private RedirectToActionResult RedirectToWarningController(RouteValueDictionary routeValueDictionary)
    {
        return RedirectToAction(
            nameof(FileUploadWarningController.Get),
            nameof(FileUploadWarningController).RemoveControllerFromName(),
            routeValueDictionary);
    }

    private RedirectToActionResult RedirectToCheckFileAndSubmitController(RouteValueDictionary routeValueDictionary)
    {
        return RedirectToAction(
            nameof(FileUploadCheckFileAndSubmitController.Get),
            nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName(),
            routeValueDictionary);
    }
}