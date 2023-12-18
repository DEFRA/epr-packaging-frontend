namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Submission;
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
    private readonly List<SubmissionPeriod> _submissionPeriods;
    private readonly int _schemeYear;

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
        _submissionPeriods = globalVariables.Value.SubmissionPeriods;
        _schemeYear = globalVariables.Value.SchemeYear;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations.First();

        var complianceSchemes = await _complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value);

        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();

        if (session.RegistrationSession.SelectedComplianceScheme == null)
        {
            session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;
            session.RegistrationSession.IsSelectedComplianceSchemeFirstCreated = IsSelectedComplianceSchemeFirstCreated(complianceSchemes, defaultComplianceScheme.Id);
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
            ComplianceSchemes = complianceSchemes,
            SubmissionPeriods = _submissionPeriods.Select(period => new DatePeriod
            {
                StartMonth = LocalisedMonthName(period.StartMonth),
                EndMonth = LocalisedMonthName(period.EndMonth)
            }).ToList()
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

        var organisation = userData.Organisations.First();

        var complianceSchemes = (await _complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id.Value)).ToList();

        if (Guid.TryParse(selectedComplianceSchemeId, out var id) && complianceSchemes.Any(x => x.Id == id))
        {
            var selectedComplianceScheme = complianceSchemes.First(s => s.Id == id);
            await _sessionManager.UpdateSessionAsync(HttpContext.Session, x =>
            {
                x.RegistrationSession.SelectedComplianceScheme = selectedComplianceScheme;
                x.RegistrationSession.IsSelectedComplianceSchemeFirstCreated = IsSelectedComplianceSchemeFirstCreated(complianceSchemes, selectedComplianceScheme.Id);
            });
        }

        return RedirectToAction(nameof(Get));
    }

    /*
     * This method is used to determine whether the selected compliance scheme is the first one that was linked with
     * the organisation. This will be used to support backwards compatibility for submissions that were created without
     * a ComplianceSchemeId property.
     */
    private static bool IsSelectedComplianceSchemeFirstCreated(List<ComplianceSchemeDto> complianceSchemes, Guid selectedComplianceSchemeId)
    {
        return complianceSchemes.MinBy(x => x.CreatedOn)?.Id == selectedComplianceSchemeId;
    }

    private async Task SaveNewJourney(FrontendSchemeRegistrationSession session)
    {
        session.SchemeMembershipSession.Journey.Clear();

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private string LocalisedMonthName(string month)
    {
        return DateTime.Parse($"1 {month} {_schemeYear}")
            .ToString("MMMM")
            .Replace("Mehefin", "Fehefin")
            .Replace("Rhagfyr", "Ragfyr");
    }
}
