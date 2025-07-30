using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Attributes;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.Options;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;

public class FrontendSchemeRegistrationController(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    ILogger<FrontendSchemeRegistrationController> logger,
    IComplianceSchemeService complianceSchemeService,
    IRegistrationApplicationService registrationApplicationService,
    IResubmissionApplicationService resubmissionApplicationService,
    IAuthorizationService authorizationService,
    INotificationService notificationService)
    : Controller
{
    [HttpGet]
    [Route(PagePaths.ApprovedPersonCreated)]
    [AuthorizeForScopes(ScopeKeySection = "FacadeAPI:DownstreamScope")]
    public async Task<IActionResult> ApprovedPersonCreated(string notification)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.RegistrationSession.NotificationMessage = notification;

        return await SaveSessionAndRedirect(
            session,
            nameof(LandingController.Get),
            nameof(LandingController).RemoveControllerFromName(),
            PagePaths.ApprovedPersonCreated,
            PagePaths.Root);
    }

    [HttpGet]
    [Route(PagePaths.LandingPage)]
    [AuthorizeForScopes(ScopeKeySection = "FacadeAPI:DownstreamScope")]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> LandingPage()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations.First(x => x.OrganisationRole == OrganisationRoles.Producer);
        var producerComplianceScheme = await complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);

        if (producerComplianceScheme is not null && authorizationService.AuthorizeAsync(User, HttpContext, PolicyConstants.EprSelectSchemePolicy).Result.Succeeded)
        {
            session.RegistrationSession.CurrentComplianceScheme = producerComplianceScheme;
            return await SaveSessionAndRedirect(session, nameof(ComplianceSchemeMemberLandingController.Get),
                nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName(), PagePaths.LandingPage,
                PagePaths.ComplianceSchemeMemberLanding);
        }

        var viewModel = new LandingPageViewModel
        {
            OrganisationName = organisation.Name!,
            OrganisationId = organisation.Id.Value,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat()
        };
        var notificationsList = await notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id!.Value);
        if (notificationsList != null)
        {
            try
            {
                viewModel.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        return View(nameof(LandingPage), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.LandingPage)]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> LandingPage(LandingPageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var userData = User.GetUserData();
            var organisation = userData.Organisations.First(x => x.OrganisationRole == OrganisationRoles.Producer);

            var notificationsList = await notificationService.GetCurrentUserNotifications(organisation.Id!.Value, userData.Id!.Value);
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

            return View(model);
        }

        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.LandingPage };
        session.RegistrationSession.IsUpdateJourney = false;
        return await SaveSessionAndRedirect(session, nameof(UsingAComplianceScheme), PagePaths.LandingPage, PagePaths.UsingAComplianceScheme);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.UsingAComplianceScheme)]
    [JourneyAccess(PagePaths.UsingAComplianceScheme)]
    public async Task<IActionResult> UsingAComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.UsingAComplianceScheme);

        var viewModel = new UsingComplianceSchemeViewModel
        {
            SavedUsingComplianceScheme = session.RegistrationSession.UsingAComplianceScheme
        };

        return View("UsingComplianceScheme", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.UsingAComplianceScheme)]
    public async Task<IActionResult> UsingAComplianceScheme(UsingComplianceSchemeViewModel model)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.UsingAComplianceScheme);
            return View("UsingComplianceScheme", model);
        }

        var usingComplianceScheme = model.UsingComplianceScheme;

        if (usingComplianceScheme.Value)
        {
            session.RegistrationSession.UsingAComplianceScheme = usingComplianceScheme.Value;
            return await SaveSessionAndRedirect(session, nameof(SelectComplianceScheme), PagePaths.UsingAComplianceScheme, PagePaths.SelectComplianceScheme);
        }

        return await SaveSessionAndRedirect(session, nameof(VisitHomePageSelfManaged), PagePaths.UsingAComplianceScheme, PagePaths.HomePageSelfManaged);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.SelectComplianceScheme)]
    [JourneyAccess(PagePaths.SelectComplianceScheme)]
    public async Task<IActionResult> SelectComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.SelectComplianceScheme);

        var viewModel = new SelectComplianceSchemeViewModel
        {
            ComplianceSchemes = await GetComplianceSchemes(),
            SavedComplianceScheme = session.RegistrationSession.SelectedComplianceScheme?.Name
        };

        return View(nameof(SelectComplianceScheme), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.SelectComplianceScheme)]
    public async Task<IActionResult> SelectComplianceScheme(SelectComplianceSchemeViewModel model)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            model.ComplianceSchemes = await GetComplianceSchemes();

            SetBackLink(session, PagePaths.SelectComplianceScheme);

            return View(nameof(SelectComplianceScheme), model);
        }

        var selectedComplianceSchemeValues = model.SelectedComplianceSchemeValues.Split(':');
        var id = Guid.Parse(selectedComplianceSchemeValues[0]);
        var schemeName = selectedComplianceSchemeValues[1];

        session.RegistrationSession.SelectedComplianceScheme = new ComplianceSchemeDto
        {
            Id = id,
            Name = schemeName
        };

        return await SaveSessionAndRedirect(
            session,
            nameof(ConfirmComplianceScheme),
            PagePaths.SelectComplianceScheme,
            PagePaths.ComplianceSchemeSelectionConfirmation);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeSelectionConfirmation)]
    [JourneyAccess(PagePaths.ComplianceSchemeSelectionConfirmation)]
    public async Task<IActionResult> ConfirmComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ComplianceSchemeSelectionConfirmation);

        var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            SelectedComplianceScheme = session.RegistrationSession.SelectedComplianceScheme,
            CurrentComplianceScheme = currentComplianceScheme,
        };

        return View("Confirmation", viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeSelectionConfirmation)]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> ConfirmComplianceScheme(ComplianceSchemeConfirmationViewModel model)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();

        var organisation = userData.Organisations.First(x => x.OrganisationRole == OrganisationRoles.Producer);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ComplianceSchemeSelectionConfirmation);
            return View("Confirmation", model);
        }

        SelectedSchemeDto result;

        if (session.RegistrationSession.IsUpdateJourney)
        {
            ProducerComplianceSchemeDto? existingComplianceScheme = null;

            if (complianceSchemeService.HasCache())
            {
                existingComplianceScheme = await complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);
            }

            result = await complianceSchemeService.ConfirmUpdateComplianceScheme(
                model.SelectedComplianceScheme.Id,
                session.RegistrationSession.CurrentComplianceScheme.SelectedSchemeId,
                organisation.Id!.Value);

            if (existingComplianceScheme?.ComplianceSchemeOperatorId.HasValue == true)
            {
                complianceSchemeService.ClearSummaryCache(existingComplianceScheme.ComplianceSchemeOperatorId.Value, existingComplianceScheme.ComplianceSchemeId);
            }
        }
        else
        {
            result = await complianceSchemeService.ConfirmAddComplianceScheme(
                model.SelectedComplianceScheme.Id,
                organisation.Id!.Value);
        }

        if (complianceSchemeService.HasCache())
        {
            var newComplianceScheme = await complianceSchemeService.GetProducerComplianceScheme(organisation.Id.Value);

            if (newComplianceScheme?.ComplianceSchemeOperatorId.HasValue == true)
            {
                await complianceSchemeService.ClearSummaryCache(newComplianceScheme.ComplianceSchemeOperatorId.Value, newComplianceScheme.ComplianceSchemeId);
            }
        }

        var producerComplianceSchemeDto = new ProducerComplianceSchemeDto
        {
            SelectedSchemeId = result.Id,
            ComplianceSchemeId = model.SelectedComplianceScheme.Id,
            ComplianceSchemeName = model.SelectedComplianceScheme.Name,
        };

        session.RegistrationSession.CurrentComplianceScheme = producerComplianceSchemeDto;
        session.RegistrationSession.UsingAComplianceScheme = null;
        session.RegistrationSession.IsUpdateJourney = false;
        session.RegistrationSession.Journey.Clear();
        session.RegistrationSession.ChangeComplianceSchemeOptions = null;

        return await SaveSessionAndRedirect(
            session,
            nameof(ComplianceSchemeMemberLandingController.Get),
            nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName(),
            PagePaths.ComplianceSchemeSelectionConfirmation,
            PagePaths.ComplianceSchemeMemberLanding);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ChangeComplianceSchemeOptions)]
    [JourneyAccess(PagePaths.ChangeComplianceSchemeOptions)]
    public async Task<IActionResult> ManageComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ChangeComplianceSchemeOptions);

        var model = new ChangeComplianceSchemeOptionsViewModel
        {
            SavedChangeComplianceSchemeOptions = session.RegistrationSession.ChangeComplianceSchemeOptions
        };
        return View("ChangeComplianceSchemeOptions", model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ChangeComplianceSchemeOptions)]
    public async Task<IActionResult> ManageComplianceScheme(ChangeComplianceSchemeOptionsViewModel model)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ChangeComplianceSchemeOptions);
            return View("ChangeComplianceSchemeOptions", model);
        }

        session.RegistrationSession.ChangeComplianceSchemeOptions = model.ChangeComplianceSchemeOptions;
        if (model.ChangeComplianceSchemeOptions == Enums.ChangeComplianceSchemeOptions.ChooseNewComplianceScheme)
        {
            session.RegistrationSession.IsUpdateJourney = true;
            return await SaveSessionAndRedirect(session, nameof(SelectComplianceScheme), PagePaths.ChangeComplianceSchemeOptions, PagePaths.SelectComplianceScheme);
        }

        return await SaveSessionAndRedirect(session, nameof(RemoveComplianceScheme), PagePaths.ChangeComplianceSchemeOptions, PagePaths.ComplianceSchemeStop);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeStop)]
    [JourneyAccess(PagePaths.ComplianceSchemeStop)]
    public async Task<IActionResult> RemoveComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ComplianceSchemeStop);

        return View("Stop");
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeStop)]
    public async Task<IActionResult> StopComplianceScheme()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ComplianceSchemeStop);
            return View("Stop");
        }

        var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
        await complianceSchemeService.StopComplianceScheme(currentComplianceScheme.SelectedSchemeId, organisation.Id!.Value);

        if (currentComplianceScheme.ComplianceSchemeOperatorId.HasValue)
        {
            await complianceSchemeService.ClearSummaryCache(
                currentComplianceScheme.ComplianceSchemeOperatorId.Value,
                currentComplianceScheme.ComplianceSchemeId);
        }

        // remove values from session
        session.RegistrationSession.CurrentComplianceScheme = null;
        session.RegistrationSession.IsUpdateJourney = false;
        session.RegistrationSession.Journey.Clear();
        session.RegistrationSession.ChangeComplianceSchemeOptions = null;

        return await SaveSessionAndRedirect(session, nameof(VisitHomePageSelfManaged), PagePaths.ComplianceSchemeStop, PagePaths.HomePageSelfManaged);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.HomePageSelfManaged)]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> VisitHomePageSelfManaged()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var producerComplianceScheme = await complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);
        var currentYear = new[] { DateTime.Now.Year.ToString(), (DateTime.Now.Year + 1).ToString() };
        var packagingResubmissionPeriod = await resubmissionApplicationService.GetActiveSubmissionPeriod();

        if (producerComplianceScheme is not null && authorizationService.AuthorizeAsync(User, HttpContext, PolicyConstants.EprSelectSchemePolicy).Result.Succeeded)
        {
            return RedirectToAction(nameof(ComplianceSchemeMemberLandingController.Get), nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName());
        }

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.SubsidiarySession.Journey.Clear();

        session.UserData = userData;
        await sessionManager.SaveSessionAsync(HttpContext.Session, session);

        var registrationApplicationPerYearViewModels = await registrationApplicationService.BuildRegistrationApplicationPerYearViewModels(HttpContext.Session, organisation);
        var resubmissionApplicationDetails = await resubmissionApplicationService.GetPackagingDataResubmissionApplicationDetails(organisation, new List<string> { packagingResubmissionPeriod?.DataPeriod }, session.RegistrationSession.SelectedComplianceScheme?.Id);

        var viewModel = new HomePageSelfManagedViewModel
        {
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            CanSelectComplianceScheme = userData.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson,
            OrganisationRole = organisation.OrganisationRole!,
            ResubmissionTaskListViewModel = resubmissionApplicationDetails.ToResubmissionTaskListViewModel(organisation),
            RegistrationApplicationsPerYear = registrationApplicationPerYearViewModels,
            PackagingResubmissionPeriod = packagingResubmissionPeriod
        };

        var notificationsList = await notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id!.Value);
        if (notificationsList != null)
        {
            try
            {
                viewModel.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        return View(nameof(HomePageSelfManaged), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.HomePageSelfManaged)]
    public async Task<IActionResult> HomePageSelfManaged()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.RegistrationSession.Journey = new List<string> { PagePaths.HomePageSelfManaged };
        return await SaveSessionAndRedirect(session, nameof(UsingAComplianceScheme), PagePaths.HomePageSelfManaged, PagePaths.UsingAComplianceScheme);
    }

    private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var index = session.RegistrationSession.Journey.IndexOf(currentPagePath);

        // this also cover if current page not found (index = -1) then it clears all pages
        session.RegistrationSession.Journey = session.RegistrationSession.Journey.Take(index + 1).ToList();
    }

    private async Task<RedirectToActionResult> SaveSessionAndRedirect(
        FrontendSchemeRegistrationSession session,
        string actionName,
        string currentPagePath,
        string? nextPagePath)
    {
        await SaveSession(session, currentPagePath, nextPagePath);

        return RedirectToAction(actionName);
    }

    private async Task<RedirectToActionResult> SaveSessionAndRedirect(
        FrontendSchemeRegistrationSession session,
        string actionName,
        string controllerName,
        string currentPagePath,
        string? nextPagePath)
    {
        await SaveSession(session, currentPagePath, nextPagePath);

        return RedirectToAction(actionName, controllerName);
    }

    private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.RegistrationSession.Journey.AddIfNotExists(nextPagePath);

        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        ViewBag.BackLinkToDisplay = session.RegistrationSession.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
    }

    private async Task<List<ComplianceSchemeDto>> GetComplianceSchemes()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        var complianceSchemes = await complianceSchemeService.GetComplianceSchemes();
        var complianceSchemesList = complianceSchemes.OrderBy(x => x.Name).ToList();
        if (session.RegistrationSession.IsUpdateJourney)
        {
            var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
            complianceSchemesList.Remove(
                complianceSchemesList.SingleOrDefault(x => x.Id == currentComplianceScheme.ComplianceSchemeId));
        }

        return complianceSchemesList;
    }
}