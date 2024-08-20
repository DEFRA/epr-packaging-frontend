namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sessions;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.ComplianceSchemeLanding)]
public class ComplianceSchemeLandingController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IComplianceSchemeService _complianceSchemeService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComplianceSchemeLandingController> _logger;

    public ComplianceSchemeLandingController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IComplianceSchemeService complianceSchemeService,
        INotificationService notificationService,
        ILogger<ComplianceSchemeLandingController> logger,
        IOptions<GlobalVariables> globalVariables)
    {
        _sessionManager = sessionManager;
        _complianceSchemeService = complianceSchemeService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        var complianceSchemes = await _complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();

        if (session.RegistrationSession.SelectedComplianceScheme == null)
        {
            session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;
        }

        await SaveNewJourney(session);

        var currentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id;

        var currentSummary = await _complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, currentComplianceSchemeId);

        var model = new ComplianceSchemeLandingViewModel
        {
            CurrentComplianceSchemeId = currentComplianceSchemeId,
            IsApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
            CurrentTabSummary = currentSummary,
            OrganisationName = organisation.Name,
            ComplianceSchemes = complianceSchemes
        };

        var notificationsList = await _notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id.Value);
        if (notificationsList != null)
        {
            try
            {
                model.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        return View("ComplianceSchemeLanding", model);
    }

    [HttpPost]
    public async Task<IActionResult> Post(string selectedComplianceSchemeId)
    {
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        var complianceSchemes = (await _complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value)).ToList();

        if (Guid.TryParse(selectedComplianceSchemeId, out var id) && complianceSchemes.Exists(x => x.Id == id))
        {
            var selectedComplianceScheme = complianceSchemes.First(s => s.Id == id);
            await _sessionManager.UpdateSessionAsync(HttpContext.Session, x =>
            {
                x.RegistrationSession.SelectedComplianceScheme = selectedComplianceScheme;
            });
        }

        return RedirectToAction(nameof(Get));
    }

    private async Task SaveNewJourney(FrontendSchemeRegistrationSession session)
    {
        session.SchemeMembershipSession.Journey.Clear();

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}
