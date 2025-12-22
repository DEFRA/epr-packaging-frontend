using System.Diagnostics.CodeAnalysis;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Controllers;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.ComplianceSchemeLanding)]
public class ComplianceSchemeLandingController(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IComplianceSchemeService complianceSchemeService,
    INotificationService notificationService,
    IRegistrationApplicationService registrationApplicationService,
    IResubmissionApplicationService resubmissionApplicationService,
    ILogger<ComplianceSchemeLandingController> logger,
    IOptions<GlobalVariables> globalVariables,
    TimeProvider? timeProvider)
    : Controller
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    [HttpGet]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> Get()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();
        var nowDateTime = _timeProvider.GetLocalNow().DateTime;

        var organisation = userData.Organisations[0];

        var complianceSchemes = await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();

        session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;
        session.UserData = userData;
        var currentYear = new[] { nowDateTime.Year.ToString(), (nowDateTime.Year + 1).ToString() };
        var packagingResubmissionPeriod = globalVariables.Value.SubmissionPeriods.Find(s => currentYear.Contains(s.Year) && s.ActiveFrom.Year == nowDateTime.Year);

        await SaveNewJourney(session);

        var currentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;

        var currentSummary = await complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, currentComplianceSchemeId);

        var resubmissionApplicationDetails = await resubmissionApplicationService.GetPackagingDataResubmissionApplicationDetails(
            organisation, new List<string> { packagingResubmissionPeriod?.DataPeriod }, session.RegistrationSession.SelectedComplianceScheme?.Id);

        var registrationApplicationPerYearViewModels = await registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);

        // Note: We are reading desired values using existing service to avoid SonarQube issue for adding 8th parameter in the constructor.
        var currentPeriod = await resubmissionApplicationService.GetCurrentMonthAndYearForRecyclingObligations();
        
        var model = new ComplianceSchemeLandingViewModel
        {
            CurrentComplianceSchemeId = currentComplianceSchemeId,
            IsApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
            CurrentTabSummary = currentSummary,
            OrganisationName = organisation.Name,
            ComplianceSchemes = complianceSchemes,
            ResubmissionTaskListViewModel = resubmissionApplicationDetails.ToResubmissionTaskListViewModel(organisation),
            RegistrationApplicationsPerYear = registrationApplicationPerYearViewModels,
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

    private async Task SaveNewJourney(FrontendSchemeRegistrationSession session)
    {
        session.SchemeMembershipSession.Journey.Clear();

        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}