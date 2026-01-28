namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.Services.Interfaces;
using ControllerExtensions;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using FrontendSchemeRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprNonRegulatorRolesPolicy)]
[Route(PagePaths.ComplianceSchemeMemberLanding)]
public class ComplianceSchemeMemberLandingController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IComplianceSchemeService _complianceSchemeService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComplianceSchemeMemberLandingController> _logger;

    public ComplianceSchemeMemberLandingController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IComplianceSchemeService complianceSchemeService,
        INotificationService notificationService,
        ILogger<ComplianceSchemeMemberLandingController> logger)
    {
        _sessionManager = sessionManager;
        _complianceSchemeService = complianceSchemeService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ??
                      new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        session.RegistrationSession.CurrentComplianceScheme =
            await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id.Value);

        if (session.RegistrationSession.CurrentComplianceScheme is null)
        {
            session.RegistrationSession.Journey.AddIfNotExists(PagePaths.HomePageSelfManaged);
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

            return RedirectToAction(
                nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged),
                nameof(FrontendSchemeRegistrationController).RemoveControllerFromName());
        }

        var model = new ComplianceSchemeMemberLandingViewModel
        {
            ComplianceSchemeName = session.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeName,
            OrganisationName = organisation.Name,
            OrganisationId = organisation.Id.Value,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            ServiceRole = userData.ServiceRole,
            CanManageComplianceScheme = userData.ServiceRole == ServiceRoles.ApprovedPerson || userData.ServiceRole == ServiceRoles.DelegatedPerson
        };

        var notificationsList =
            await _notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id.Value);
        if (notificationsList != null)
        {
            try
            {
                model.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message,
                    userData.Id.Value, organisation.Id.Value);
            }
        }

        // add this landing page to journey so back button behaviour has previous page to navigate to
        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.ComplianceSchemeMemberLanding);
        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.ChangeComplianceSchemeOptions);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return View("ComplianceSchemeMemberLanding", model);
    }
}