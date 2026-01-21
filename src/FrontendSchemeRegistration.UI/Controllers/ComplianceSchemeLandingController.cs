using System.Diagnostics.CodeAnalysis;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
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
    IResubmissionApplicationService resubmissionApplicationService,
    ILogger<ComplianceSchemeLandingController> logger,
    TimeProvider timeProvider)
    : Controller
{
    private TimeProvider _timeProvider = timeProvider;

    public void SetTestTimeProvider(TimeProvider testTimeProvider)
    {
        _timeProvider = testTimeProvider;
    }

    [HttpGet]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> Get()
    {
        var userData = User.GetUserData();
        var nowDateTime = _timeProvider.GetLocalNow().DateTime;

        var organisation = userData.Organisations[0];
        var complianceSchemes = await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        //build minimal session data to remove any pollution from previous journeys
        var session = SetupMinimalSession.FrontendSchemeRegistrationSession(complianceSchemes, userData);
        var taskSave = sessionManager.SaveSessionAsync(HttpContext.Session, session);

        var currentYear = new[] { nowDateTime.Year.ToString(), (nowDateTime.Year + 1).ToString() };
        // Note: We are reading desired values using existing service to avoid SonarQube issue for adding 8th parameter in the constructor.
        var currentPeriod = await resubmissionApplicationService.GetCurrentMonthAndYearForRecyclingObligations(_timeProvider);
        // Note: We are adding a service method here to avoid SonarQube issue for adding 8th parameter in the constructor.
        var packagingResubmissionPeriod = resubmissionApplicationService.PackagingResubmissionPeriod(currentYear, nowDateTime);
        
        var currentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;
        await taskSave;

        var currentSummary = await complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, currentComplianceSchemeId);

        var resubmissionApplicationDetails = await resubmissionApplicationService.GetPackagingDataResubmissionApplicationDetails(
            organisation, new List<string> { packagingResubmissionPeriod.DataPeriod }, session.RegistrationSession.SelectedComplianceScheme?.Id);
        
        var model = new ComplianceSchemeLandingViewModel
        {
            CurrentComplianceSchemeId = currentComplianceSchemeId,
            IsApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
            CurrentTabSummary = currentSummary,
            OrganisationName = organisation.Name,
            ComplianceSchemes = complianceSchemes,
            ResubmissionTaskListViewModel = resubmissionApplicationDetails.ToResubmissionTaskListViewModel(organisation),
            PackagingResubmissionPeriod = packagingResubmissionPeriod,
            ComplianceYear = currentPeriod.currentMonth == 1 ? (currentPeriod.currentYear - 1).ToString() : currentPeriod.currentYear.ToString() // this is a temp fix for the compliance window change
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
}