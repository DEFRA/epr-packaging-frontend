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

using Application.Extensions;

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
    [HttpGet]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> Get()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();
        var now = timeProvider.GetLocalNow().DateTime;

        var organisation = userData.Organisations[0];

        var complianceSchemes = await complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();

        session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;
        session.UserData = userData;
        var currentYear = new[] { now.Year.ToString(), (now.Year + 1).ToString() };
        // Note: We are adding a service method here to avoid SonarQube issue for adding 8th parameter in the constructor.
        var packagingResubmissionPeriod = resubmissionApplicationService.PackagingResubmissionPeriod(currentYear, now);
        
        await SaveNewJourney(session);

        var currentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;

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
            ComplianceYear = now.GetComplianceYear().ToString()
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