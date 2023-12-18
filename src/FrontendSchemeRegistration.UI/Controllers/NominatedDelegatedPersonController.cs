namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs;
using Application.Options;
using Application.Services.Interfaces;
using Attributes;
using Constants;
using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sessions;
using ViewModels;

public class NominatedDelegatedPersonController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly GlobalVariables _globalVariables;
    private readonly IRoleManagementService _roleManagementService;
    private readonly INotificationService _notificationService;

    public NominatedDelegatedPersonController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables,
        IRoleManagementService roleManagementService,
        INotificationService notificationService)
    {
        _sessionManager = sessionManager;
        _globalVariables = globalVariables.Value;
        _roleManagementService = roleManagementService;
        _notificationService = notificationService;
    }

    [HttpGet]
    [Route(PagePaths.InviteChangePermissions + "/{id:guid}")]
    [JourneyAccess(PagePaths.InviteChangePermissions, JourneyName.NominatedDelegatedPersonStart)]
    public async Task<IActionResult> InviteChangePermissions(Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = ClaimsExtensions.GetUserData(User);
        var organisation = userData.Organisations.Single();

        if(session == null)
        {
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.InviteChangePermissions}/{id}";
        var nextPagePath = $"{PagePaths.TelephoneNumber}/{id}";

        var response = await _roleManagementService.GetDelegatedPersonNominator(id, organisation.Id);

        var model = new InviteChangePermissionsViewModel
        {
            Id = id,
            Firstname = response.FirstName,
            Lastname = response.LastName,
            OrganisationName = response.OrganisationName
        };

        StartJourney(session, currentPagePath, nextPagePath);

        SetBackLink(session, currentPagePath);

        session.NominatedDelegatedPersonSession.NominatorFullName = response.FirstName + " " + response.LastName;

        session.NominatedDelegatedPersonSession.OrganisationName = response.OrganisationName;

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return View(nameof(InviteChangePermissions), model);
    }

    [HttpGet]
    [Route(PagePaths.TelephoneNumber + "/{id:guid}")]
    [JourneyAccess(PagePaths.TelephoneNumber, JourneyName.NominatedDelegatedPerson)]
    public async Task<IActionResult> TelephoneNumber(Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session == null)
        {
            return RedirectHome();
        }

        var userData = ClaimsExtensions.GetUserData(User);
        var currentPagePath = $"{PagePaths.TelephoneNumber}/{id}";

        SetBackLink(session, currentPagePath);

        var model = new TelephoneNumberViewModel
        {
            EmailAddress = userData.Email,
            EnrolmentId = id,
            TelephoneNumber = session.NominatedDelegatedPersonSession?.TelephoneNumber
        };
        return View(nameof(TelephoneNumber), model);
    }

    [HttpPost]
    [Route(PagePaths.TelephoneNumber + "/{id:guid}")]
    [JourneyAccess(PagePaths.TelephoneNumber, JourneyName.NominatedDelegatedPerson)]
    public async Task<IActionResult> TelephoneNumber(TelephoneNumberViewModel model, Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session == null)
        {
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.TelephoneNumber}/{id}";
        var nextPagePath = $"{PagePaths.ConfirmPermissionSubmitData}/{id}";

        if (!ModelState.IsValid)
        {
            var userData = ClaimsExtensions.GetUserData(User);
            model.EmailAddress = userData.Email;
            model.EnrolmentId = id;

            SetBackLink(session, currentPagePath);

            return View(model);
        }

        session.NominatedDelegatedPersonSession.TelephoneNumber = model.TelephoneNumber;

        return await SaveSessionAndRedirect(session, nameof(ConfirmPermissionSubmitData), currentPagePath, nextPagePath, id);
    }

    [HttpGet]
    [Route(PagePaths.ConfirmPermissionSubmitData + "/{id:guid}")]
    [JourneyAccess(PagePaths.ConfirmPermissionSubmitData, JourneyName.NominatedDelegatedPerson)]
    public async Task<IActionResult> ConfirmPermissionSubmitData(Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session == null)
        {
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.ConfirmPermissionSubmitData}/{id}";

        SetBackLink(session, currentPagePath);

        var model = new NominationAcceptanceModel
        {
            EnrolmentId = id,
            NominatorFullName = session.NominatedDelegatedPersonSession.NominatorFullName,
            NomineeFullName = session.NominatedDelegatedPersonSession.NomineeFullName,
            OrganisationName = session.NominatedDelegatedPersonSession.OrganisationName
        };

        return View(nameof(ConfirmPermissionSubmitData), model);
    }

    [HttpPost]
    [Route(PagePaths.ConfirmPermissionSubmitData + "/{id:guid}")]
    [JourneyAccess(PagePaths.ConfirmPermissionSubmitData, JourneyName.NominatedDelegatedPerson)]
    public async Task<IActionResult> ConfirmPermissionSubmitData(NominationAcceptanceModel model, Guid id)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session?.NominatedDelegatedPersonSession == null)
        {
            return RedirectHome();
        }

        var organisationId = User.GetOrganisationId();

        if (!organisationId.HasValue)
        {
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.ConfirmPermissionSubmitData}/{id}";

        if (!ModelState.IsValid)
        {
            model.EnrolmentId = id;
            model.NominatorFullName = session.NominatedDelegatedPersonSession.NominatorFullName;
            model.NomineeFullName = session.NominatedDelegatedPersonSession.NomineeFullName;
            model.OrganisationName = session.NominatedDelegatedPersonSession.OrganisationName;

            SetBackLink(session, currentPagePath);

            return View(model);
        }

        session.NominatedDelegatedPersonSession.NomineeFullName = model.NomineeFullName;

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        await _roleManagementService.AcceptNominationToDelegatedPerson(
            enrolmentId: id,
            organisationId: organisationId.Value,
            serviceKey: "Packaging",
            acceptNominationRequest: new AcceptNominationRequest
            {
                NomineeDeclaration = session.NominatedDelegatedPersonSession.NomineeFullName,
                Telephone = session.NominatedDelegatedPersonSession.TelephoneNumber
            });

        await _notificationService.ResetCache(organisationId.Value, User.UserId());

        return RedirectHome();
    }

    private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var index = session.NominatedDelegatedPersonSession.Journey.IndexOf(currentPagePath);
        session.NominatedDelegatedPersonSession.Journey = session.NominatedDelegatedPersonSession.Journey.Take(index + 1).ToList();
    }

    private async Task<RedirectToActionResult> SaveSessionAndRedirect(
        FrontendSchemeRegistrationSession session,
        string actionName,
        string currentPagePath,
        string? nextPagePath,
        Guid id)
    {
        await SaveSession(session, currentPagePath, nextPagePath);

        return RedirectToAction(actionName, new { id });
    }

    private async Task StartJourney(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        session.NominatedDelegatedPersonSession.Journey.Clear();

        session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(string.Empty);
        session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(currentPagePath);
        session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(nextPagePath);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(nextPagePath);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var previousPage = session.NominatedDelegatedPersonSession.Journey.PreviousOrDefault(currentPagePath);
        ViewBag.BackLinkToDisplay = previousPage != null ? $"{_globalVariables.BasePath}{previousPage}" : string.Empty;
    }

    private RedirectToActionResult RedirectHome()
    {
        return RedirectToAction("Get", "Landing");
    }
}
