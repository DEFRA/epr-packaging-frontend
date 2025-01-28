namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.Application.DTOs.Submission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sessions;
using System.Diagnostics.CodeAnalysis;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.ComplianceSchemeLanding)]
public class ComplianceSchemeLandingController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IComplianceSchemeService _complianceSchemeService;
    private readonly INotificationService _notificationService;
    private readonly ISubmissionService _submissionService;
    private readonly DateTime _configurableDeadline;
    private readonly ILogger<ComplianceSchemeLandingController> _logger;

    //this is wrong needs fixing 
    private static readonly string SubmissionYear = DateTime.Now.Year.ToString();
    private readonly SubmissionPeriod _period = new() { DataPeriod = $"January to December {SubmissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{SubmissionYear}" };

    public ComplianceSchemeLandingController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IComplianceSchemeService complianceSchemeService,
        INotificationService notificationService,
        ISubmissionService submissionService,
        ILogger<ComplianceSchemeLandingController> logger,
        IOptions<GlobalVariables> globalVariables)
    {
        _sessionManager = sessionManager;
        _complianceSchemeService = complianceSchemeService;
        _notificationService = notificationService;
        _logger = logger;
        _submissionService = submissionService;
        _configurableDeadline = globalVariables.Value.ApplicationDeadline;
    }

    [HttpGet]
    [ExcludeFromCodeCoverage]
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

        await GetRegistrationApplicationStatus(session);

        var model = new ComplianceSchemeLandingViewModel
        {
            CurrentComplianceSchemeId = currentComplianceSchemeId,
            IsApprovedUser = userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved),
            CurrentTabSummary = currentSummary,
            OrganisationName = organisation.Name,
            ComplianceSchemes = complianceSchemes,
            ApplicationStatus = session.RegistrationApplicationSession?.ApplicationStatus,
            ApplicationReferenceNumber = session.RegistrationApplicationSession?.ApplicationReferenceNumber,
            FileUploadStatus = session.RegistrationApplicationSession?.FileUploadStatus,
            PaymentViewStatus = session.RegistrationApplicationSession?.PaymentViewStatus,
            AdditionalDetailsStatus = session.RegistrationApplicationSession?.AdditionalDetailsStatus
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

        session.SubsidiarySession.Journey.Clear();

        return View("ComplianceSchemeLanding", model);
    }

    private async Task GetRegistrationApplicationStatus(FrontendSchemeRegistrationSession session)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var request = new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id!.Value,
            SubmissionPeriod = _period.DataPeriod,
            ComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme?.Id
        };

        var response = await _submissionService.GetRegistrationApplicationDetails(request);

        await RegistrationApplicationDetailsExtension.GetRegistrationApplicationStatus(session, response, _configurableDeadline);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
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
