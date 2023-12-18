namespace FrontendSchemeRegistration.UI.Controllers;

using System.Linq;
using Application.Constants;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using Attributes;
using Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Sessions;

[FeatureGate(FeatureFlags.ShowComplianceSchemeMemberManagement)]
public class SchemeMembershipController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly GlobalVariables _globalVariables;
    private readonly IComplianceSchemeMemberService _complianceSchemeMemberService;
    private readonly SiteDateOptions _siteDateOptions;
    private readonly int _pageSize;
    private readonly ILogger<SchemeMembershipController> _logger;

    public SchemeMembershipController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IOptions<GlobalVariables> globalVariables,
        IComplianceSchemeMemberService complianceSchemeMemberService,
        ILogger<SchemeMembershipController> logger,
        IOptions<SiteDateOptions> siteDateOptions,
        IOptions<ComplianceSchemeMembersPaginationOptions> complianceSchemeMembersPaginationOptions)
    {
        _sessionManager = sessionManager;
        _globalVariables = globalVariables.Value;
        _complianceSchemeMemberService = complianceSchemeMemberService;
        _siteDateOptions = siteDateOptions.Value;
        _pageSize = complianceSchemeMembersPaginationOptions.Value.PageSize;
        _logger = logger;
    }

    [HttpGet]
    [Route(PagePaths.SchemeMembers + "/{complianceSchemeId:guid}")]
    public async Task<IActionResult> SchemeMembers(Guid complianceSchemeId, string search = "", int page = 1)
    {
        if (page < 1)
        {
            return RedirectToAction(actionName: "SchemeMembers", controllerName: "SchemeMembership",
                new { complianceSchemeId, search, page = 1 });
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session == null)
        {
            _logger.LogError("Session is null");
            return RedirectHome();
        }

        var userData = User.GetUserData();
        var organisation = userData.Organisations.SingleOrDefault();
        if (organisation == null)
        {
            _logger.LogError("Organisation is null");
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.SchemeMembers}/{complianceSchemeId}?search={search}&page={page}";

        InitialiseJourney(session, currentPagePath);

        ViewBag.HomeLinkToDisplay = _globalVariables.BasePath;

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        var complianceSchemeMembership = await _complianceSchemeMemberService
            .GetComplianceSchemeMembers(organisation.Id.Value, complianceSchemeId, _pageSize, search, page);

        if (complianceSchemeMembership == null)
        {
            _logger.LogError(
                "No scheme found for user '{userID}' in organisation '{organisationId}' for scheme '{id}' - redirecting to home page",
                userData.Id.Value, organisation.Id.Value, complianceSchemeId);
            return RedirectHome();
        }

        if (page > 1 && complianceSchemeMembership.PagedResult.PageCount < page)
        {
            _logger.LogInformation(
                "Page '{page}' requested but results only contains '{complianceSchemeMembership.PagedResult.PageCount}' pages. Redirect to first page",
                page, complianceSchemeMembership.PagedResult.PageCount);
            return RedirectToAction(actionName: "SchemeMembers", controllerName: "SchemeMembership",
                new { complianceSchemeId, search, page = 1 });
        }

        if (complianceSchemeMembership.LinkedOrganisationCount == 0)
        {
            _logger.LogInformation(
                "Organisation '{organisationId}' for scheme '{id}' has no members - redirecting to home page",
                organisation.Id.Value, complianceSchemeId);
            return RedirectHome();
        }

        var schemeMembersModel = new SchemeMembersModel
        {
            Id = complianceSchemeId,
            Name = complianceSchemeMembership.SchemeName,
            MemberCount = complianceSchemeMembership.LinkedOrganisationCount.ToString(),
            LastUpdated = complianceSchemeMembership.LastUpdated.Value.ToString(_siteDateOptions.DateFormat),
            PagingDetail =
            {
                TotalItems = complianceSchemeMembership.PagedResult.TotalItems,
                CurrentPage = complianceSchemeMembership.PagedResult.CurrentPage,
                PageSize = complianceSchemeMembership.PagedResult.PageSize
            },
            SearchText = search
        };

        var pageUrl = Url.Action("SchemeMembers", new { complianceSchemeId });
        schemeMembersModel.PagingDetail.PagingLink = $"{pageUrl}?search={search}&page=";

        if (!string.IsNullOrWhiteSpace(search))
        {
            schemeMembersModel.ResetLink = Url.Action("SchemeMembers", new { complianceSchemeId });
        }

        foreach (var item in complianceSchemeMembership.PagedResult.Items)
        {
            var memberItem = (item.OrganisationNumber.ToReferenceNumberFormat(), item.OrganisationName,
                Url.Action("MemberDetails", new { selectedSchemeId = item.SelectedSchemeId }));
            schemeMembersModel.MemberList.Add(memberItem);
        }

        return View(nameof(SchemeMembers), schemeMembersModel);
    }

    [HttpGet]
    [Route(PagePaths.MemberDetails + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.MemberDetails, JourneyName.SchemeMembershipStart)]
    public async Task<IActionResult> MemberDetails(Guid selectedSchemeId)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session == null)
        {
            _logger.LogError("Session is null");
            return RedirectHome();
        }

        var userData = User.GetUserData();
        var organisation = userData.Organisations.SingleOrDefault();
        if (organisation == null)
        {
            _logger.LogError("Organisation is null");
            return RedirectHome();
        }

        ResetMemberRemovalDetails(session);

        var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
        if (complianceSchemeMember is null)
        {
            _logger.LogError("ComplianceSchemeMember is null");
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.MemberDetails}/{selectedSchemeId}";
        var nextPagePath = $"{PagePaths.ReasonsForRemoval}/{selectedSchemeId}";

        session.SchemeMembershipSession.Journey.AddIfNotExists(currentPagePath);
        SaveSession(session, currentPagePath, nextPagePath);
        SetBackLink(session, currentPagePath);

        var model = new MemberDetailsViewModel
        {
            OrganisationName = complianceSchemeMember.OrganisationName,
            OrganisationNumber = complianceSchemeMember.OrganisationNumber.ToReferenceNumberFormat(),
            RegisteredNation = complianceSchemeMember.RegisteredNation,
            OrganisationType = complianceSchemeMember.ProducerType,
            CompanyHouseNumber = complianceSchemeMember.CompanyHouseNumber,
            ComplianceScheme = complianceSchemeMember.ComplianceScheme,
            SelectedSchemeId = selectedSchemeId,
            ShowRemoveLink = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved)
        };

        return View(nameof(MemberDetails), model);
    }

    [HttpGet]
    [Route(PagePaths.ReasonsForRemoval + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.ReasonsForRemoval, JourneyName.SchemeMembership)]
    public async Task<IActionResult> ReasonsForRemoval(Guid selectedSchemeId)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();
        var organisation = userData.Organisations.Single();
        var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
        if (complianceSchemeMember is null)
        {
            return RedirectHome();
        }

        if (session == null)
        {
            _logger.LogError("Session is null");
            return RedirectHome();
        }

        var currentPagePath = $"{PagePaths.ReasonsForRemoval}/{selectedSchemeId}";
        var nextPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";

        SetBackLink(session, currentPagePath);

        var viewModel = new ReasonForRemovalViewModel()
        {
            ReasonForRemoval = await _complianceSchemeMemberService.GetReasonsForRemoval(),
            OrganisationName = complianceSchemeMember.OrganisationName,
            SelectedReasonForRemoval = session.SchemeMembershipSession.SelectedReasonForRemoval,
            IsApprovedUser = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved)
        };

        return View(nameof(ReasonsForRemoval), viewModel);
    }

    [HttpPost]
    [Route(PagePaths.ReasonsForRemoval + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.ReasonsForRemoval, JourneyName.SchemeMembership)]
    public async Task<IActionResult> ReasonsForRemoval(Guid selectedSchemeId, ReasonForRemovalViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();
        var organisation = userData.Organisations.Single();

        var currentPagePath = $"{PagePaths.ReasonsForRemoval}/{selectedSchemeId}";
        var nextPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";

        SetBackLink(session, currentPagePath);

        model.IsApprovedUser = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved);

        if (!model.IsApprovedUser)
        {
            return new UnauthorizedResult();
        }

        ModelState.Remove("OrganisationName");
        ModelState.Remove("ReasonForRemoval");

        if (session.SchemeMembershipSession.SelectedReasonForRemoval != model.SelectedReasonForRemoval)
        {
            session.SchemeMembershipSession.TellUsMore = null;
        }

        session.SchemeMembershipSession.SelectedReasonForRemoval = model.SelectedReasonForRemoval;
        model.ReasonForRemoval = await _complianceSchemeMemberService.GetReasonsForRemoval();

        if (!ModelState.IsValid)
        {
            var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
            if (complianceSchemeMember is null)
            {
                return RedirectHome();
            }

            model.OrganisationName = complianceSchemeMember.OrganisationName;
            return View(model);
        }

        session.SchemeMembershipSession.SelectedReasonForRemoval = model.SelectedReasonForRemoval;
        bool selectedCode = model.ReasonForRemoval.Any(s => s.Code == session.SchemeMembershipSession.SelectedReasonForRemoval);

        if (!selectedCode)
        {
            ModelState.AddModelError(nameof(ReasonsForRemoval), "ReasonForRemoval.Error");
            var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
            if (complianceSchemeMember is null)
            {
                return RedirectHome();
            }

            model.OrganisationName = complianceSchemeMember.OrganisationName;
            return View(model);
        }

        var selectedCodeRequiresReason = model.ReasonForRemoval.First(s => s.Code == session.SchemeMembershipSession.SelectedReasonForRemoval);

        if (selectedCodeRequiresReason.RequiresReason == true)
        {
            nextPagePath = $"{PagePaths.TellUsMore}/{selectedSchemeId}";
            return await SaveSessionAndRedirect(session, nameof(TellUsMore), currentPagePath, nextPagePath, selectedSchemeId);
        }

        nextPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";
        return await SaveSessionAndRedirect(session, nameof(ConfirmRemoval), currentPagePath, nextPagePath, selectedSchemeId);
    }

    [HttpGet]
    [Route(PagePaths.TellUsMore + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.TellUsMore, JourneyName.SchemeMembership)]
    public async Task<IActionResult> TellUsMore(Guid selectedSchemeId)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session == null)
        {
            return RedirectHome();
        }

        if (!session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved))
        {
            return new UnauthorizedResult();
        }

        var currentPagePath = $"{PagePaths.TellUsMore}/{selectedSchemeId}";
        var nextPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";

        SetBackLink(session, currentPagePath);

        var viewModel = new RemovalTellUsMoreViewModel()
        {
            SelectedReasonForRemoval = session.SchemeMembershipSession.SelectedReasonForRemoval,
            TellUsMore = session.SchemeMembershipSession.TellUsMore
        };

        return View(nameof(TellUsMore), viewModel);
    }

    [HttpPost]
    [Route(PagePaths.TellUsMore + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.TellUsMore, JourneyName.SchemeMembership)]
    public async Task<IActionResult> TellUsMore(Guid selectedSchemeId, RemovalTellUsMoreViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var currentPagePath = $"{PagePaths.TellUsMore}/{selectedSchemeId}";
        var nextPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";

        SetBackLink(session, currentPagePath);

        ModelState.Remove("SelectedReasonForRemoval");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        session.SchemeMembershipSession.TellUsMore = model.TellUsMore;

        return await SaveSessionAndRedirect(session, nameof(ConfirmRemoval), currentPagePath, nextPagePath, selectedSchemeId);
    }

    [HttpGet]
    [Route(PagePaths.ConfirmRemoval + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.ConfirmRemoval, JourneyName.SchemeMembership)]
    public async Task<IActionResult> ConfirmRemoval(Guid selectedSchemeId)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = ClaimsExtensions.GetUserData(User);
        var organisation = userData.Organisations.Single();

        if (session == null)
        {
            return RedirectHome();
        }

        if (!session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved))
        {
            return new UnauthorizedResult();
        }

        var currentPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";
        SetBackLink(session, currentPagePath);

        var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
        if(complianceSchemeMember is null)
        {
            return RedirectHome();
        }

        var model = new ConfirmRemovalViewModel
        {
            OrganisationName = complianceSchemeMember.OrganisationName
        };
        return View(nameof(ConfirmRemoval), model);
    }

    [HttpPost]
    [Route(PagePaths.ConfirmRemoval + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.ConfirmRemoval, JourneyName.SchemeMembership)]
    public async Task<IActionResult> ConfirmRemoval(Guid selectedSchemeId, ConfirmRemovalViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = ClaimsExtensions.GetUserData(User);
        var organisation = userData.Organisations.Single();
        var currentPagePath = $"{PagePaths.ConfirmRemoval}/{selectedSchemeId}";
        ModelState.Remove("OrganisationName");
        SetBackLink(session, currentPagePath);
        if (!ModelState.IsValid)
        {
            var complianceSchemeMember = await _complianceSchemeMemberService.GetComplianceSchemeMemberDetails(organisation.Id.Value, selectedSchemeId);
            if(complianceSchemeMember is null)
            {
                return RedirectHome();
            }

            model.OrganisationName = complianceSchemeMember.OrganisationName;
            return View(model);
        }

        if (model.SelectedConfirmRemoval.Value == YesNoAnswer.Yes)
        {
            var nextPagePath = $"{PagePaths.ConfirmationOfRemoval}/{selectedSchemeId}";

            var removedSchemeMember = await _complianceSchemeMemberService.RemoveComplianceSchemeMember(
                organisation.Id.Value,
                session.RegistrationSession.SelectedComplianceScheme.Id,
                selectedSchemeId,
                session.SchemeMembershipSession.SelectedReasonForRemoval,
                session.SchemeMembershipSession.TellUsMore);

            session.SchemeMembershipSession.RemovedSchemeMember = removedSchemeMember.OrganisationName;

            return await SaveSessionAndRedirect(session, nameof(ConfirmationOfRemoval), currentPagePath, nextPagePath, selectedSchemeId);
        }

        if (model.SelectedConfirmRemoval.Value == YesNoAnswer.No)
        {
            var nextPagePath = $"{PagePaths.MemberDetails}/{selectedSchemeId}";
            return await SaveSessionAndRedirect(session, nameof(MemberDetails), currentPagePath, nextPagePath, selectedSchemeId);
        }

        return RedirectHome();
    }

    [HttpGet]
    [Route(PagePaths.ConfirmationOfRemoval + "/{selectedSchemeId:guid}")]
    [JourneyAccess(PagePaths.ConfirmationOfRemoval, JourneyName.SchemeMembership)]
    public async Task<IActionResult> ConfirmationOfRemoval(Guid selectedSchemeId)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session == null)
        {
            return RedirectHome();
        }

        if (!session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved))
        {
            return new UnauthorizedResult();
        }

        var model = new ConfirmationOfRemovalViewModel
        {
            OrganisationName = string.IsNullOrEmpty(session.SchemeMembershipSession.RemovedSchemeMember) ? string.Empty : session.SchemeMembershipSession.RemovedSchemeMember,
            CurrentComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme.Id
        };
        return View(nameof(ConfirmationOfRemoval), model);
    }

    private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var index = session.SchemeMembershipSession.Journey.IndexOf(currentPagePath);
        if (index != -1)
        {
            session.SchemeMembershipSession.Journey = session.SchemeMembershipSession.Journey.Take(index + 1).ToList();
        }
    }

    private static void ResetMemberRemovalDetails(FrontendSchemeRegistrationSession session)
    {
        session.SchemeMembershipSession.SelectedReasonForRemoval = null;
        session.SchemeMembershipSession.TellUsMore = null;
    }

    private async Task<RedirectToActionResult> SaveSessionAndRedirect(
        FrontendSchemeRegistrationSession session,
        string actionName,
        string currentPagePath,
        string? nextPagePath,
        Guid id)
    {
        await SaveSession(session, currentPagePath, nextPagePath);

        return RedirectToAction(actionName, new { selectedSchemeId = id });
    }

    private async Task InitialiseJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        session.SchemeMembershipSession.Journey.Clear();

        session.SchemeMembershipSession.Journey.Add(currentPagePath);

        ResetMemberRemovalDetails(session);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.SchemeMembershipSession.Journey.AddIfNotExists(nextPagePath);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var previousPage = session.SchemeMembershipSession.Journey.PreviousOrDefault(currentPagePath);
        ViewBag.BackLinkToDisplay = previousPage != null ? $"{_globalVariables.BasePath}{previousPage}" : string.Empty;
    }

    private RedirectToActionResult RedirectHome()
    {
        return RedirectToAction("Get", "Landing");
    }
}
