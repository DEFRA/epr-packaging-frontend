using System.Diagnostics.CodeAnalysis;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.ComplianceSchemeLanding)]
public class ComplianceSchemeLandingController(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IComplianceSchemeService complianceSchemeService,
    INotificationService notificationService,
    IRegistrationApplicationService registrationApplicationService,
    ISubmissionService submissionService,
    ILogger<ComplianceSchemeLandingController> logger)
    : Controller
{
    private readonly string _packagingResubmissionPeriod = "July to December 2024";

    [HttpGet]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> Get()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        var complianceSchemes = await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();

        session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;

        await SaveNewJourney(session);

        var currentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;

        var currentSummary = await complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, currentComplianceSchemeId);

        var registrationApplicationSession = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation);

        var latestSubmissionDetails = await submissionService.GetSubmissionsAsync<PomSubmission>(
            new List<string>() { _packagingResubmissionPeriod }, 1, session.RegistrationSession.SelectedComplianceScheme?.Id);

        var model = new ComplianceSchemeLandingViewModel
        {
            CurrentComplianceSchemeId = currentComplianceSchemeId,
            IsApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
            CurrentTabSummary = currentSummary,
            OrganisationName = organisation.Name,
            ComplianceSchemes = complianceSchemes,
            ApplicationStatus = registrationApplicationSession.ApplicationStatus,
            ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
            FileUploadStatus = registrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
            IsResubmission = registrationApplicationSession.IsResubmission,
            ResubmissionTaskListViewModel = GetResubmissionTaskListViewModel(latestSubmissionDetails)
        };

        var notificationsList = await notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id.Value);
        if (notificationsList != null)
        {
            try
            {
                model.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        session.SubsidiarySession.Journey.Clear();

        return View("ComplianceSchemeLanding", model);
    }

    [HttpPost]
    public async Task<IActionResult> Post(string selectedComplianceSchemeId)
    {
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        var complianceSchemes = (await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value)).ToList();

        if (Guid.TryParse(selectedComplianceSchemeId, out var id) && complianceSchemes.Exists(x => x.Id == id))
        {
            var selectedComplianceScheme = complianceSchemes.First(s => s.Id == id);
            await sessionManager.UpdateSessionAsync(HttpContext.Session, x => { x.RegistrationSession.SelectedComplianceScheme = selectedComplianceScheme; });
        }

        return RedirectToAction(nameof(Get));
    }

    public ResubmissionTaskListViewModel GetResubmissionTaskListViewModel(List<PomSubmission?> submissions)
    {
        var viewModel = new ResubmissionTaskListViewModel();

        if (submissions != null && submissions.Count > 0)
        {
            var submission = submissions.FirstOrDefault();

            if (submission != null)
            {
                viewModel.IsSubmitted = submission.IsSubmitted;
                viewModel.IsResubmissionInProgress = submission.IsResubmissionInProgress;
                viewModel.IsResubmissionComplete = submission.IsResubmissionComplete;
                viewModel.AppReferenceNumber = submission.AppReferenceNumber;
            }
        }

        return viewModel;
    }

    private async Task SaveNewJourney(FrontendSchemeRegistrationSession session)
    {
        session.SchemeMembershipSession.Journey.Clear();

        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}