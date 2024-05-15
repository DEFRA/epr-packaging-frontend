using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FrontendSchemeRegistration.UI.Controllers
{
    public class NominatedApprovedPersonController : Controller
    {
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
        private readonly INotificationService _notificationService;
        private readonly IRoleManagementService _roleManagementService;
        private readonly GlobalVariables _globalVariables;

        public NominatedApprovedPersonController(
            ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
            IOptions<GlobalVariables> globalVariables,
            INotificationService notificationService,
            IRoleManagementService roleManagementService)
        {
            _sessionManager = sessionManager;
            _notificationService = notificationService;
            _roleManagementService = roleManagementService;
            _globalVariables = globalVariables.Value;
        }

        [HttpGet]
        [Route(PagePaths.InviteChangePermissionsAP + "/{id:guid}")]
        public async Task<IActionResult> InviteChangePermissions(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var userData = ClaimsExtensions.GetUserData(User);
            var organisation = userData.Organisations.Single();
            var model = new InviteChangePermissionsViewModel()
            {
                OrganisationName = organisation.Name,
                IsInCompaniesHouse = organisation.OrganisationType.IsCompaniesHouseCompany(),
                Id = id,
            };

            var currentPagePath = $"{PagePaths.InviteChangePermissionsAP}/{id}";
            var nextPagePath = model.IsInCompaniesHouse ? $"{PagePaths.RoleInOrganisation}/{id}" : $"{PagePaths.ManualInputRoleInOrganisation}/{id}";
            StartJourney(session, currentPagePath, nextPagePath);
            SetBackLink(session, currentPagePath);
            return View(nameof(InviteChangePermissions), model);
        }

        [HttpGet]
        [Route(PagePaths.RoleInOrganisation + "/{id:guid}")]
        public async Task<IActionResult> RoleInOrganisation(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var model = new RoleInOrganisationViewModel
            {
                Id = id,
            };

            if (!session.NominatedApprovedPersonSession.RoleInOrganisation.IsNullOrEmpty())
            {
                model.RoleInOrganisation = (RoleInOrganisation)Enum.Parse(typeof(RoleInOrganisation), session.NominatedApprovedPersonSession.RoleInOrganisation);
            }

            var currentPagePath = $"{PagePaths.RoleInOrganisation}/{id}";
            SetBackLink(session, currentPagePath);

            return View(nameof(RoleInOrganisation), model);
        }

        [HttpPost]
        [Route(PagePaths.RoleInOrganisation + "/{id:guid}")]
        public async Task<IActionResult> RoleInOrganisation(RoleInOrganisationViewModel model, Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var currentPagePath = $"{PagePaths.RoleInOrganisation}/{id}";
            if (!ModelState.IsValid)
            {
                SetBackLink(session, currentPagePath);

                return View(nameof(RoleInOrganisation), model);
            }

            var nextPagePath = $"{PagePaths.TelephoneNumberAP}/{id}";
            SaveSession(session, currentPagePath, nextPagePath);

            session.NominatedApprovedPersonSession.RoleInOrganisation = model.RoleInOrganisation.ToString();
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
            return RedirectToAction(nameof(TelephoneNumber), new { id = id });
        }

        [HttpGet]
        [Route(PagePaths.ManualInputRoleInOrganisation + "/{id:guid}")]
        public async Task<IActionResult> ManualRoleInOrganisation(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var model = new ManualRoleInOrganisationViewModel()
            {
                RoleInOrganisation = session.NominatedApprovedPersonSession.RoleInOrganisation,
                Id = id
            };
            var currentPagePath = $"{PagePaths.ManualInputRoleInOrganisation}/{id}";
            SetBackLink(session, currentPagePath);
            return View(nameof(ManualRoleInOrganisation), model);
        }

        [HttpPost]
        [Route(PagePaths.ManualInputRoleInOrganisation + "/{id:guid}")]
        public async Task<IActionResult> ManualRoleInOrganisation(ManualRoleInOrganisationViewModel model, Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var currentPagePath = $"{PagePaths.ManualInputRoleInOrganisation}/{id}";
            if (!ModelState.IsValid)
            {
                SetBackLink(session, currentPagePath);

                return View(nameof(ManualRoleInOrganisation), model);
            }

            var nextPagePath = $"{PagePaths.TelephoneNumberAP}/{id}";
            SaveSession(session, currentPagePath, nextPagePath);

            session.NominatedApprovedPersonSession.RoleInOrganisation = model.RoleInOrganisation;
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
            return RedirectToAction(nameof(TelephoneNumber), new { id = id });
        }

        [HttpGet]
        [Route(PagePaths.TelephoneNumberAP + "/{id:guid}")]
        public async Task<IActionResult> TelephoneNumber(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var userData = ClaimsExtensions.GetUserData(User);
            var model = new TelephoneNumberAPViewModel()
            {
                EmailAddress = userData.Email,
                TelephoneNumber = session.NominatedApprovedPersonSession.TelephoneNumber,
                Id = id
            };
            var currentPagePath = $"{PagePaths.TelephoneNumberAP}/{id}";
            SetBackLink(session, currentPagePath);
            return View(nameof(TelephoneNumber), model);
        }

        [HttpPost]
        [Route(PagePaths.TelephoneNumberAP + "/{id:guid}")]
        public async Task<IActionResult> TelephoneNumber(TelephoneNumberAPViewModel model, Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var currentPagePath = $"{PagePaths.TelephoneNumberAP}/{id}";

            if (!ModelState.IsValid)
            {
                SetBackLink(session, currentPagePath);
                return View(nameof(TelephoneNumber), model);
            }

            var nextPagePath = $"{PagePaths.ConfirmDetailsAP}/{id}";
            SaveSession(session, currentPagePath, nextPagePath);
            session.NominatedApprovedPersonSession.TelephoneNumber = model.TelephoneNumber;
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
            return RedirectToAction(nameof(ConfirmDetails), new { Id = id });
        }

        [HttpGet]
        [Route(PagePaths.ConfirmDetailsAP + "/{id:guid}")]
        public async Task<IActionResult> ConfirmDetails(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var userData = ClaimsExtensions.GetUserData(User);
            var organisation = userData.Organisations.Single();

            var isInCompaniesHouse = organisation.OrganisationType.IsCompaniesHouseCompany();
            var roleChangeUrl = isInCompaniesHouse ? $"{_globalVariables.BasePath}{PagePaths.RoleInOrganisation}/{id}" : $"{_globalVariables.BasePath}{PagePaths.ManualInputRoleInOrganisation}/{id}";

            var model = new ConfirmDetailsApprovedPersonViewModel()
            {
                Id = id,
                TelephoneNumber = session.NominatedApprovedPersonSession.TelephoneNumber,
                RoleInOrganisation = session.NominatedApprovedPersonSession.RoleInOrganisation,
                RoleChangeUrl = roleChangeUrl,
                TelephoneChangeUrl = $"{_globalVariables.BasePath}{PagePaths.TelephoneNumberAP}/{id}"
            };
            var currentPagePath = $"{PagePaths.ConfirmDetailsAP}/{id}";
            var nextPagePath = $"{PagePaths.DeclarationWithFullNameAP}/{id}";
            SaveSession(session, currentPagePath, nextPagePath);
            SetBackLink(session, currentPagePath);
            return View(nameof(ConfirmDetails), model);
        }

        [HttpGet]
        [Route(PagePaths.DeclarationWithFullNameAP + "/{id:guid}")]
        public async Task<IActionResult> Declaration(Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var userData = ClaimsExtensions.GetUserData(User);
            var organisation = userData.Organisations.Single();
            var model = new DeclarationApprovedPersonViewModel()
            {
                OrganisationName = organisation.Name,
                Id = id
            };
            var currentPagePath = $"{PagePaths.DeclarationWithFullNameAP}/{id}";
            SetBackLink(session, currentPagePath);
            return View(nameof(Declaration), model);
        }

        [HttpPost]
        [Route(PagePaths.DeclarationWithFullNameAP + "/{id:guid}")]
        public async Task<IActionResult> Declaration(DeclarationApprovedPersonViewModel model, Guid id)
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
            if (session == null)
            {
                return RedirectHome();
            }

            var organisationId = User.GetOrganisationId();

            var currentPagePath = $"{PagePaths.DeclarationWithFullNameAP}/{id}";
            if (!ModelState.IsValid)
            {
                SetBackLink(session, currentPagePath);
                return View(nameof(Declaration), model);
            }

            session.NominatedApprovedPersonSession.IsNominationSubmittedSuccessfully = true;
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

            var userData = User.GetUserData();
            var organisation = userData.Organisations.First();
            await _roleManagementService.AcceptNominationToApprovedPerson(enrolmentId: id, organisationId: organisationId.Value, serviceKey: "Packaging",
                acceptApprovedPersonRequest: new AcceptApprovedPersonRequest
                {
                    DeclarationFullName = model.DeclarationFullName,
                    Telephone = session.NominatedApprovedPersonSession.TelephoneNumber,
                    JobTitle = session.NominatedApprovedPersonSession.RoleInOrganisation,
                    DeclarationTimeStamp = DateTime.Now,
                    OrganisationNumber = organisation.OrganisationNumber,
                    OrganisationName = organisation.Name,
                    PersonFirstName = userData.FirstName,
                    PersonLastName = userData.LastName,
                    ContactEmail = userData.Email
                });

            await _notificationService.ResetCache(organisationId.Value, userData.Id.Value);

            return RedirectHome();
        }

        private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
        {
            var index = session.NominatedDelegatedPersonSession.Journey.IndexOf(currentPagePath);
            session.NominatedDelegatedPersonSession.Journey = session.NominatedDelegatedPersonSession.Journey.Take(index + 1).ToList();
        }

        private async Task StartJourney(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
        {
            session.NominatedDelegatedPersonSession.Journey.Clear();

            session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(string.Empty);
            session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(currentPagePath);
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

        private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
        {
            ClearRestOfJourney(session, currentPagePath);

            session.NominatedDelegatedPersonSession.Journey.AddIfNotExists(nextPagePath);

            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
        }
    }
}
