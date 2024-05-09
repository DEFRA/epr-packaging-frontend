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
[Route(PagePaths.FileUploadCompanyDetailsSubLanding)]
public class FileUploadCompanyDetailsSubLandingController : Controller
{
    private const int _submissionsLimit = 1;
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly List<SubmissionPeriod> _submissionPeriods;
    private readonly string _basePath;

    public FileUploadCompanyDetailsSubLandingController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _basePath = globalVariables.Value.BasePath;
        _submissionPeriods = globalVariables.Value.SubmissionPeriods;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        ViewBag.HomeLinkToDisplay = _basePath;
        var periods = _submissionPeriods.Select(x => x.DataPeriod).ToList();
        var submissions = new List<RegistrationSubmission>();
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        foreach (var period in periods)
        {
            var submission = await _submissionService.GetSubmissionsAsync<RegistrationSubmission>(
                new List<string> { period },
                _submissionsLimit,
                session.RegistrationSession.SelectedComplianceScheme?.Id);
            submissions.AddRange(submission);
        }

        var submissionPeriodDetails = new List<SubmissionPeriodDetail>();

        foreach (var submissionPeriod in _submissionPeriods)
        {
            var submission = submissions.FirstOrDefault(x => x.SubmissionPeriod == submissionPeriod.DataPeriod);
            submissionPeriodDetails.Add(new SubmissionPeriodDetail
            {
                DataPeriod = submissionPeriod.DataPeriod,
                Deadline = submissionPeriod.Deadline,
                Status = DateTime.Now < submissionPeriod.ActiveFrom ?
                        SubmissionPeriodStatus.CannotStartYet : submission?.GetSubmissionStatus() ?? SubmissionPeriodStatus.NotStarted
            });
        }

        if (session is not null)
        {
            session.RegistrationSession.Journey = ResetUploadJourney(session.RegistrationSession.Journey);
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

            var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;
            if (organisationRole is not null)
            {
                return View(
                    "FileUploadCompanyDetailsSubLanding",
                    new FileUploadCompanyDetailsSubLandingViewModel
                    {
                        SubmissionPeriodDetails = submissionPeriodDetails,
                        ComplianceSchemeName = session.RegistrationSession.SelectedComplianceScheme?.Name,
                        OrganisationRole = organisationRole
                    });
            }
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
        session.RegistrationSession.Journey.Add(PagePaths.FileUploadCompanyDetailsSubLanding);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        var submissions = await _submissionService.GetSubmissionsAsync<RegistrationSubmission>(
            new List<string> { selectedSubmissionPeriod.DataPeriod },
            _submissionsLimit,
            session.RegistrationSession.SelectedComplianceScheme?.Id);
        var submission = submissions.FirstOrDefault();

        if (submission != null)
        {
            var submissionStatus = submission.GetSubmissionStatus();

            switch (submissionStatus)
            {
                case SubmissionPeriodStatus.FileUploaded
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.FileUploaded when session.UserData.ServiceRole.Parse<ServiceRole>()
                    .In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(ReviewCompanyDetailsController.Get),
                        nameof(ReviewCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.SubmittedToRegulator
                    when session.UserData.ServiceRole.Parse<ServiceRole>()
                        .In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload
                    when session.UserData.ServiceRole.Parse<ServiceRole>()
                        .In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(ReviewCompanyDetailsController.Get),
                        nameof(ReviewCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.SubmittedToRegulator
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.NotStarted:
                    return RedirectToAction(
                        nameof(FileUploadCompanyDetailsController.Get),
                        nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(),
                        null);
            }
        }

        return RedirectToAction(
            nameof(FileUploadCompanyDetailsController.Get),
            nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(),
            null);
    }

    private static List<string> ResetUploadJourney(List<string> journey)
    {
        List<string> journeyPointers = new List<string>
        {
            PagePaths.FileUploadCompanyDetailsSubLanding,
            PagePaths.FileUploadCompanyDetails,
            PagePaths.FileUploadBrands,
            PagePaths.FileUploadPartnerships
        };

        journey.RemoveAll(j => journeyPointers.Contains(j));

        return journey;
    }
}