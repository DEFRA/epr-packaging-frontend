using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Attributes;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Resources.Views.ChangeComplianceSchemeOptions;
using FrontendSchemeRegistration.UI.Resources.Views.Compliance;
using FrontendSchemeRegistration.UI.Resources.Views.ComplianceSchemeStop;
using FrontendSchemeRegistration.UI.Resources.Views.UsingComplianceScheme;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;

public class FrontendSchemeRegistrationController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IComplianceSchemeService _complianceSchemeService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;
    private readonly ISubmissionService _submissionService;
    private readonly IPaymentCalculationService _paymentCalculationService;
	private readonly ILogger<FrontendSchemeRegistrationController> _logger;
    private readonly DateTime _configurableDeadline;
    //this is wrong needs fixing 
    private static readonly string SubmissionYear = DateTime.Now.Year.ToString();
    private readonly SubmissionPeriod _period = new() { DataPeriod = $"January to December {SubmissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{SubmissionYear}" };

    public FrontendSchemeRegistrationController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<FrontendSchemeRegistrationController> logger,
        IComplianceSchemeService complianceSchemeService,
        IAuthorizationService authorizationService,
        INotificationService notificationService,
        ISubmissionService submissionService,
        IOptions<GlobalVariables> globalVariables,
        IPaymentCalculationService paymentCalculationService)
    {
        _sessionManager = sessionManager;
        _complianceSchemeService = complianceSchemeService;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        _submissionService = submissionService;
        _paymentCalculationService = paymentCalculationService;
        _configurableDeadline = globalVariables.Value.ApplicationDeadline;
        _logger = logger;
    }

    [HttpGet]
    [Route(PagePaths.LandingPage)]
    [AuthorizeForScopes(ScopeKeySection = "FacadeAPI:DownstreamScope")]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> LandingPage()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var userData = User.GetUserData();

        var organisation = userData.Organisations.First(x => x.OrganisationRole == OrganisationRoles.Producer);
        var producerComplianceScheme = await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);

        if (producerComplianceScheme is not null && _authorizationService.AuthorizeAsync(User, HttpContext, PolicyConstants.EprSelectSchemePolicy).Result.Succeeded)
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
        var notificationsList = await _notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id!.Value);
        if (notificationsList != null)
        {
            try
            {
                viewModel.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        return View(nameof(LandingPage), viewModel);
    }

    [HttpGet]
    [Route(PagePaths.ApprovedPersonCreated)]
    [AuthorizeForScopes(ScopeKeySection = "FacadeAPI:DownstreamScope")]
    public async Task<IActionResult> ApprovedPersonCreated(string notification)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.RegistrationSession.NotificationMessage = notification;

        return await SaveSessionAndRedirect(
            session,
            nameof(LandingController.Get),
            nameof(LandingController).RemoveControllerFromName(),
            PagePaths.ApprovedPersonCreated,
            PagePaths.Root);
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

            var notificationsList = await _notificationService.GetCurrentUserNotifications(organisation.Id!.Value, userData.Id!.Value);
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

            return View(model);
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.UsingAComplianceScheme);

        var viewModel = new UsingComplianceSchemeViewModel
        {
            SavedUsingComplianceScheme = session.RegistrationSession?.UsingAComplianceScheme
        };

        return View(nameof(UsingComplianceScheme), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.UsingAComplianceScheme)]
    public async Task<IActionResult> UsingAComplianceScheme(UsingComplianceSchemeViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.UsingAComplianceScheme);
            return View(nameof(UsingComplianceScheme), model);
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.SelectComplianceScheme);

        var viewModel = new SelectComplianceSchemeViewModel
        {
            ComplianceSchemes = await GetComplianceSchemes(),
            SavedComplianceScheme = session.RegistrationSession?.SelectedComplianceScheme?.Name
        };

        return View(nameof(SelectComplianceScheme), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.SelectComplianceScheme)]
    public async Task<IActionResult> SelectComplianceScheme(SelectComplianceSchemeViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

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
            Name = schemeName,
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ComplianceSchemeSelectionConfirmation);

        var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            SelectedComplianceScheme = session.RegistrationSession.SelectedComplianceScheme,
            CurrentComplianceScheme = currentComplianceScheme,
        };

        return View(nameof(Confirmation), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeSelectionConfirmation)]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> ConfirmComplianceScheme(ComplianceSchemeConfirmationViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();

        var organisation = userData.Organisations.First(x => x.OrganisationRole == OrganisationRoles.Producer);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ComplianceSchemeSelectionConfirmation);
            return View(nameof(Confirmation), model);
        }

        SelectedSchemeDto result;

        if (session.RegistrationSession.IsUpdateJourney)
        {
            ProducerComplianceSchemeDto? existingComplianceScheme = null;

            if (_complianceSchemeService.HasCache())
            {
                existingComplianceScheme = await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);
            }

            result = await _complianceSchemeService.ConfirmUpdateComplianceScheme(
                model.SelectedComplianceScheme.Id,
                session.RegistrationSession.CurrentComplianceScheme.SelectedSchemeId,
                organisation.Id!.Value);

            if (existingComplianceScheme?.ComplianceSchemeOperatorId.HasValue == true)
            {
                _complianceSchemeService.ClearSummaryCache(existingComplianceScheme.ComplianceSchemeOperatorId.Value, existingComplianceScheme.ComplianceSchemeId);
            }
        }
        else
        {
            result = await _complianceSchemeService.ConfirmAddComplianceScheme(
                model.SelectedComplianceScheme.Id,
                organisation.Id!.Value);
        }

        if (_complianceSchemeService.HasCache())
        {
            var newComplianceScheme = await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id.Value);

            if (newComplianceScheme?.ComplianceSchemeOperatorId.HasValue == true)
            {
                await _complianceSchemeService.ClearSummaryCache(newComplianceScheme.ComplianceSchemeOperatorId.Value, newComplianceScheme.ComplianceSchemeId);
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ChangeComplianceSchemeOptions);

        var model = new ChangeComplianceSchemeOptionsViewModel
        {
            SavedChangeComplianceSchemeOptions = session.RegistrationSession.ChangeComplianceSchemeOptions
        };
        return View(nameof(ChangeComplianceSchemeOptions), model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ChangeComplianceSchemeOptions)]
    public async Task<IActionResult> ManageComplianceScheme(ChangeComplianceSchemeOptionsViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ChangeComplianceSchemeOptions);
            return View(nameof(ChangeComplianceSchemeOptions), model);
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        SetBackLink(session, PagePaths.ComplianceSchemeStop);

        return View(nameof(Stop));
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.ComplianceSchemeStop)]
    public async Task<IActionResult> StopComplianceScheme()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var userData = User.GetUserData();

        var organisation = userData.Organisations[0];

        if (!ModelState.IsValid)
        {
            SetBackLink(session, PagePaths.ComplianceSchemeStop);
            return View(nameof(Stop));
        }

        var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
        await _complianceSchemeService.StopComplianceScheme(currentComplianceScheme.SelectedSchemeId, organisation.Id!.Value);

        if (currentComplianceScheme.ComplianceSchemeOperatorId.HasValue)
        {
            await _complianceSchemeService.ClearSummaryCache(
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

    private async Task GetRegistrationTaskListStatus(FrontendSchemeRegistrationSession session)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var response = await _submissionService.GetRegistrationApplicationDetails(
            new GetRegistrationApplicationDetailsRequest
            {
                OrganisationNumber = int.Parse(organisation.OrganisationNumber),
                OrganisationId = organisation.Id!.Value,
                SubmissionPeriod = _period.DataPeriod,
                ComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme?.Id
            });

        await RegistrationApplicationDetailsExtension.GetRegistrationApplicationStatus(session, response, _configurableDeadline);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.HomePageSelfManaged)]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> VisitHomePageSelfManaged()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var producerComplianceScheme = await _complianceSchemeService.GetProducerComplianceScheme(organisation.Id!.Value);

        if (producerComplianceScheme is not null && _authorizationService.AuthorizeAsync(User, HttpContext, PolicyConstants.EprSelectSchemePolicy).Result.Succeeded)
        {
            return RedirectToAction(nameof(ComplianceSchemeMemberLandingController.Get), nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName());
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.SubsidiarySession.Journey.Clear();

        await GetRegistrationTaskListStatus(session);

        var viewModel = new HomePageSelfManagedViewModel
        {
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            CanSelectComplianceScheme = userData.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson,
            OrganisationRole = organisation.OrganisationRole!,
            ApplicationStatus = session.RegistrationApplicationSession.ApplicationStatus,
            FileUploadStatus = session.RegistrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = session.RegistrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = session.RegistrationApplicationSession.AdditionalDetailsStatus,
            ApplicationReferenceNumber = session.RegistrationApplicationSession.ApplicationReferenceNumber,
            RegistrationReferenceNumber = session.RegistrationApplicationSession.RegistrationReferenceNumber
        };

        var notificationsList = await _notificationService.GetCurrentUserNotifications(organisation.Id.Value, userData.Id!.Value);
        if (notificationsList != null)
        {
            try
            {
                viewModel.Notification.BuildFromNotificationList(notificationsList);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }
        
        return View(nameof(HomePageSelfManaged), viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.HomePageSelfManaged)]
    public async Task<IActionResult> HomePageSelfManaged()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.RegistrationSession.Journey = new List<string> { PagePaths.HomePageSelfManaged };
        return await SaveSessionAndRedirect(session, nameof(UsingAComplianceScheme), PagePaths.HomePageSelfManaged, PagePaths.UsingAComplianceScheme);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.ProducerRegistrationGuidance)]
    public async Task<IActionResult> ProducerRegistrationGuidance()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var complianceSchemes = await _complianceSchemeService.GetOperatorComplianceSchemes(organisation.Id!.Value);

        session.RegistrationSession.SelectedComplianceScheme ??= complianceSchemes.FirstOrDefault();

        session.RegistrationSession.Journey = new List<string> { PagePaths.HomePageSelfManaged, PagePaths.ProducerRegistrationGuidance };

        await GetRegistrationTaskListStatus(session);

        if (session.RegistrationApplicationSession.ApplicationStatus is ApplicationStatusType.FileUploaded or ApplicationStatusType.SubmittedAndHasRecentFileUpload ||
            session.RegistrationApplicationSession.FileUploadStatus is RegistrationTaskListStatus.Pending or RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        if (string.IsNullOrEmpty(session.RegistrationApplicationSession.RegulatorNation))
        {
            var organisationId = isComplianceScheme ? session.RegistrationSession.SelectedComplianceScheme?.Id : organisation.Id;
            session.RegistrationApplicationSession.RegulatorNation = await _paymentCalculationService.GetRegulatorNation(organisationId);
        }

        return View(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = session.RegistrationApplicationSession.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = userData.Organisations[0].OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.RegistrationSession.SelectedComplianceScheme?.Name!
        });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationTaskList)]
    public async Task<IActionResult> RegistrationTaskList()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> {
            isComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.RegistrationTaskList
        };

        SetBackLink(session, PagePaths.RegistrationTaskList);

        await GetRegistrationTaskListStatus(session);

        if (session.RegistrationApplicationSession.SubmissionId is not null && session.RegistrationApplicationSession.IsSubmitted && string.IsNullOrWhiteSpace(session.RegistrationApplicationSession.ApplicationReferenceNumber) && session.RegistrationApplicationSession.LastSubmittedFile.FileId is not null)
        {
            int csRowNumber = isComplianceScheme ? session.RegistrationSession.SelectedComplianceScheme.RowNumber : 0;
            session.RegistrationApplicationSession.ApplicationReferenceNumber = _paymentCalculationService.CreateApplicationReferenceNumber(
                                                                                 isComplianceScheme, csRowNumber, organisation.OrganisationNumber!, _period);
            await _submissionService.SubmitAsync(session.RegistrationApplicationSession.SubmissionId.Value, session.RegistrationApplicationSession.LastSubmittedFile.FileId.Value, session.RegistrationApplicationSession.LastSubmittedFile.SubmittedByName!, session.RegistrationApplicationSession.ApplicationReferenceNumber);
        }

        if (string.IsNullOrEmpty(session.RegistrationApplicationSession.RegulatorNation))
        {
            var organisationId = isComplianceScheme ? session.RegistrationSession.SelectedComplianceScheme?.Id : organisation.Id;
            session.RegistrationApplicationSession.RegulatorNation = await _paymentCalculationService.GetRegulatorNation(organisationId);
        }

        await SaveSession(session, PagePaths.RegistrationTaskList, PagePaths.RegistrationFeeCalculations);

        return View(new RegistrationTaskListViewModel
        {
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            FileReachedSynapse = session.RegistrationApplicationSession.FileReachedSynapse,
            ApplicationStatus = session.RegistrationApplicationSession.ApplicationStatus,
            FileUploadStatus = session.RegistrationApplicationSession.FileUploadStatus,
            PaymentViewStatus = session.RegistrationApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = session.RegistrationApplicationSession.AdditionalDetailsStatus
        });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationFeeCalculations)]
    public async Task<IActionResult> RegistrationFeeCalculations()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.RegistrationTaskList, PagePaths.RegistrationFeeCalculations };
        SetBackLink(session, PagePaths.RegistrationFeeCalculations);

        if (session.RegistrationApplicationSession.FileUploadStatus is not RegistrationTaskListStatus.Completed)
        {
            _logger.LogWarning("RegistrationApplicationSession.FileUploadStatus is not Completed for ApplicationReferenceNumber {Number}", session.RegistrationApplicationSession.ApplicationReferenceNumber);
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        if (organisation.OrganisationRole == "Compliance Scheme")
        {
            _logger.LogInformation("getting Compliance Scheme Details for organisation number {Number}", organisation.OrganisationNumber!);
            //var complianceSchemeDetails = await _paymentCalculationService.GetComplianceSchemeDetails(organisation.OrganisationNumber!);
            var complianceSchemeDetails = session.RegistrationApplicationSession.CsoMemberDetails;

            _logger.LogInformation("getting ComplianceSchemeRegistrationFees for ApplicationReferenceNumber {Number}", session.RegistrationApplicationSession.ApplicationReferenceNumber);
            var response = await _paymentCalculationService.GetComplianceSchemeRegistrationFees(complianceSchemeDetails, session.RegistrationApplicationSession.ApplicationReferenceNumber, organisation.Id);

            if (response == null)
            {
                _logger.LogWarning("Error in Getting ComplianceSchemeRegistrationFees for ApplicationReferenceNumber {Number}", session.RegistrationApplicationSession.ApplicationReferenceNumber);
                return RedirectToAction(nameof(RegistrationTaskList));
            }

            session.RegistrationApplicationSession.TotalAmountOutstanding = response.OutstandingPayment;
            await SaveSession(session, PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions);

            var individualProducerData = response.ComplianceSchemeMembersWithFees.GetIndividualProducers(complianceSchemeDetails.Members);

            int smallProducersFee = individualProducerData.smallProducers.GetFees();
            int smallProducersCount = individualProducerData.smallProducers.Count;

            int largeProducersFee = individualProducerData.largeProducers.GetFees();
            int largeProducersCount = individualProducerData.largeProducers.Count;

            var onlineMarketplaces = response.ComplianceSchemeMembersWithFees.GetOnlineMarketPlaces();
            var lateProducersFees = response.ComplianceSchemeMembersWithFees.GetLateProducers();

            var subsidiaryCompaniesFees = response.ComplianceSchemeMembersWithFees.GetSubsidiariesCompanies();
            var subsidiaryCompaniesCount = session.RegistrationApplicationSession.CsoMemberDetails.Members.Sum(dto => dto.NumberOfSubsidiaries);

            return View("ComplianceSchemeRegistrationFeeCalculations", new ComplianceSchemeFeeCalculationBreakdownViewModel
            {
                RegistrationFee = response.ComplianceSchemeRegistrationFee,
                SmallProducersFee = smallProducersFee,
                SmallProducersCount = smallProducersCount,
                LargeProducersFee = largeProducersFee,
                LargeProducersCount = largeProducersCount,
                OnlineMarketplaceFee = onlineMarketplaces.Sum(),
                OnlineMarketplaceCount = onlineMarketplaces.Count,
                SubsidiaryCompanyFee = subsidiaryCompaniesFees.Sum(),
                SubsidiaryCompanyCount = subsidiaryCompaniesCount,
                LateProducerFee = lateProducersFees.Sum(),
                LateProducersCount = lateProducersFees.Count,
                TotalPreviousPayments = response.PreviousPayment,
                TotalFeeAmount = response.TotalFee,
                RegistrationFeePaid = session.RegistrationApplicationSession.RegistrationFeePaid,
                RegistrationApplicationSubmitted = session.RegistrationApplicationSession.RegistrationApplicationSubmitted,
            });
        }
        else
        {
            var response = await _paymentCalculationService.GetProducerRegistrationFees(
            session.RegistrationApplicationSession.ProducerDetails!,
            session.RegistrationApplicationSession.ApplicationReferenceNumber!,
            session.RegistrationApplicationSession.IsLateFeeApplicable,
            organisation.Id,
            session.RegistrationApplicationSession.LastSubmittedFile.SubmittedDateTime!.Value);

            if (response == null)
            {
                _logger.LogWarning("Error in Getting ProducerRegistrationFees for ApplicationReferenceNumber {Number}", session.RegistrationApplicationSession.ApplicationReferenceNumber);
                return RedirectToAction(nameof(RegistrationTaskList));
            }

            session.RegistrationApplicationSession.TotalAmountOutstanding = response.OutstandingPayment;
            await SaveSession(session, PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions);

            if (session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed)
            {
                await _submissionService.SubmitRegistrationApplicationAsync(
                    session.RegistrationApplicationSession.SubmissionId.Value,
                    session.RegistrationSession.SelectedComplianceScheme?.Id, null, "PayOnline",
                    session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                    SubmissionType.RegistrationFeePayment);
            }

            return View(new FeeCalculationBreakdownViewModel
            {
                ProducerSize = session.RegistrationApplicationSession.ProducerDetails!.ProducerSize,
                IsOnlineMarketplace = session.RegistrationApplicationSession.ProducerDetails!.IsOnlineMarketplace,
                NumberOfSubsidiaries = session.RegistrationApplicationSession.ProducerDetails!.NumberOfSubsidiaries,
                NumberOfSubsidiariesBeingOnlineMarketplace = session.RegistrationApplicationSession.ProducerDetails!.NumberOfSubsidiariesBeingOnlineMarketPlace,
                IsLateFeeApplicable = session.RegistrationApplicationSession.IsLateFeeApplicable,
                BaseFee = response.ProducerRegistrationFee,
                OnlineMarketplaceFee = response.ProducerOnlineMarketPlaceFee,
                TotalSubsidiaryFee = response.SubsidiariesFee - response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalSubsidiaryOnlineMarketplaceFee = response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalPreviousPayments = response.PreviousPayment,
                TotalFeeAmount = response.TotalFee,
                RegistrationFeePaid = session.RegistrationApplicationSession.RegistrationFeePaid,
                ProducerLateRegistrationFee = response.ProducerLateRegistrationFee,
                RegistrationApplicationSubmitted = session.RegistrationApplicationSession.RegistrationApplicationSubmitted,
            });
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.RegistrationTaskList, PagePaths.AdditionalInformation };

        if (session.RegistrationApplicationSession.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed &&
            session.RegistrationApplicationSession.ApplicationStatus is 
            ApplicationStatusType.AcceptedByRegulator or 
            ApplicationStatusType.ApprovedByRegulator or 
            ApplicationStatusType.SubmittedToRegulator)
        {
            return RedirectToAction(nameof(SubmitRegistrationRequest));
        }

        SetBackLink(session, PagePaths.AdditionalInformation);
        if (session.RegistrationApplicationSession.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed ||
            session.RegistrationApplicationSession.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }
        
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        return View(new AdditionalInformationViewModel
        {
            RegulatorNation = session.RegistrationApplicationSession.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.RegistrationSession.SelectedComplianceScheme?.Name!
        });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation(AdditionalInformationViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        var isAuthorisedUser = session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved);
        if (!isAuthorisedUser)
        {
            return RedirectToAction(nameof(UnauthorisedUserWarnings));
        }

        if (!session.RegistrationApplicationSession.RegistrationApplicationSubmitted && session.RegistrationApplicationSession.FileUploadStatus == RegistrationTaskListStatus.Completed && session.RegistrationApplicationSession.PaymentViewStatus == RegistrationTaskListStatus.Completed)
        {
            await _submissionService.SubmitRegistrationApplicationAsync(session.RegistrationApplicationSession.SubmissionId!.Value, session.RegistrationSession.SelectedComplianceScheme?.Id, model.AdditionalInformationText, null, session.RegistrationApplicationSession.ApplicationReferenceNumber!, SubmissionType.RegistrationApplicationSubmitted);
        }

        return RedirectToAction(nameof(SubmitRegistrationRequest));
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.UnauthorisedUserWarnings)]
    public async Task<IActionResult> UnauthorisedUserWarnings()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.AdditionalInformation, PagePaths.UnauthorisedUserWarnings };

        SetBackLink(session, PagePaths.UnauthorisedUserWarnings);

        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        ViewBag.IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        if (ViewBag.IsComplianceScheme)
        {
            ViewBag.ComplianceScheme = session.RegistrationSession.SelectedComplianceScheme?.Name;
            ViewBag.NationName = NationExtensions.GetNationName(session.RegistrationApplicationSession.RegulatorNation).ToLower();
        }
        else
        {
            ViewBag.OrganisationName = organisation.Name;
            ViewBag.OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat();
        }

        return View("UnauthorisedUserWarnings");
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.SubmitRegistrationRequest)]
    public async Task<IActionResult> SubmitRegistrationRequest()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if(session.RegistrationApplicationSession.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            session.RegistrationSession.Journey = new List<string> { PagePaths.RegistrationTaskList, PagePaths.SubmitRegistrationRequest };
            SetBackLink(session, PagePaths.SubmitRegistrationRequest);
        }

        return View("ApplicationSubmissionConfirmation",
            new ApplicationSubmissionConfirmationViewModel
            {
                RegulatorNation = session.RegistrationApplicationSession.RegulatorNation,
                ApplicationReferenceNumber = session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                RegistrationReferenceNumber = session.RegistrationApplicationSession.RegistrationReferenceNumber!,
                ApplicationStatus = session.RegistrationApplicationSession.ApplicationStatus,
                RegistrationApplicationSubmittedDate = session.RegistrationApplicationSession.RegistrationApplicationSubmittedDate
            }
        );
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions };

        var model = new SelectPaymentOptionsViewModel
        {
            RegulatorNation = session.RegistrationApplicationSession.RegulatorNation,
            TotalAmountOutstanding = session.RegistrationApplicationSession.TotalAmountOutstanding,
        };
        if (!model.IsEngland)
        {
            return RedirectToAction(nameof(PayByBankTransfer));
        }

        if (session.RegistrationApplicationSession.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        SetBackLink(session, PagePaths.SelectPaymentOptions);

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions(SelectPaymentOptionsViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        SetBackLink(session, PagePaths.SelectPaymentOptions);

        model.RegulatorNation = session.RegistrationApplicationSession.RegulatorNation;
        model.TotalAmountOutstanding = session.RegistrationApplicationSession.TotalAmountOutstanding;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (model.PaymentOption)
        {
            case (int)PaymentOptions.PayOnline:
                return RedirectToAction(nameof(PayOnline));
            case (int)PaymentOptions.PayByBankTransfer:
                return RedirectToAction(nameof(PayByBankTransfer));
            case (int)PaymentOptions.PayByPhone:
                return RedirectToAction(nameof(PayByPhone));
            default: return View(model);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByPhone)]
    public async Task<IActionResult> PayByPhone()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session == null || session.RegistrationSession == null || string.IsNullOrWhiteSpace(session.RegistrationApplicationSession.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        session.RegistrationSession.Journey = new List<string> { PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone };
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone);

        if (session.RegistrationApplicationSession.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        return View("PaymentOptionPayByPhone",
            new PaymentOptionPayByPhoneViewModel
            {
                TotalAmountOutstanding = session.RegistrationApplicationSession.TotalAmountOutstanding,
                ApplicationReferenceNumber = session.RegistrationApplicationSession.ApplicationReferenceNumber!
            });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByPhone)]
    public async Task<IActionResult> PayByPhoneSaveSession()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await _submissionService.SubmitRegistrationApplicationAsync(
                session.RegistrationApplicationSession.SubmissionId.Value,
                session.RegistrationSession.SelectedComplianceScheme?.Id, null, "PayByPhone",
                session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                SubmissionType.RegistrationFeePayment);
        }

        return await RedirectToLandingPage(session);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnline()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        session.RegistrationSession.Journey = new List<string> { PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayOnline };

        SetBackLink(session, PagePaths.PaymentOptionPayOnline);

        if (session.RegistrationApplicationSession.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        var paymentLink = await InitiatePayment(session);

        if (string.IsNullOrWhiteSpace(paymentLink))
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        if (session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await _submissionService.SubmitRegistrationApplicationAsync(session.RegistrationApplicationSession.SubmissionId.Value, session.RegistrationSession.SelectedComplianceScheme?.Id, null, "PayOnline", session.RegistrationApplicationSession.ApplicationReferenceNumber!, SubmissionType.RegistrationFeePayment);
        }

        return View("PaymentOptionPayOnline",
            new PaymentOptionPayOnlineViewModel
            {
                TotalAmountOutstanding = session.RegistrationApplicationSession.TotalAmountOutstanding,
                ApplicationReferenceNumber = session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                PaymentLink = paymentLink
            });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnlineSaveSession(PaymentOptionPayOnlineViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await _submissionService.SubmitRegistrationApplicationAsync(
                session.RegistrationApplicationSession.SubmissionId.Value, 
                session.RegistrationSession.SelectedComplianceScheme?.Id, null, "PayOnline",
                session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                SubmissionType.RegistrationFeePayment);
        }

        return await RedirectToLandingPage(session);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransfer()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session == null || session.RegistrationSession == null || string.IsNullOrWhiteSpace(session.RegistrationApplicationSession.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }
        session.RegistrationSession.Journey = new List<string> { PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer };

        if (session.RegistrationApplicationSession.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        var model = new PaymentOptionPayByBankTransferViewModel
        {
            RegulatorNation = session.RegistrationApplicationSession.RegulatorNation,
            ApplicationReferenceNumber = session.RegistrationApplicationSession.ApplicationReferenceNumber!,
            TotalAmountOutstanding = session.RegistrationApplicationSession.TotalAmountOutstanding
        };

        if (!model.IsEngland)
        {
            await SetOrReplaceBackLink(session, PagePaths.PaymentOptionPayByBankTransfer, PagePaths.SelectPaymentOptions, PagePaths.RegistrationFeeCalculations);
        }
        else
        {
            SetBackLink(session, PagePaths.PaymentOptionPayByBankTransfer);
        }

        await SaveSession(session, PagePaths.PaymentOptionPayByBankTransfer, null);

        return View("PaymentOptionPayByBankTransfer", model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransferSaveSession()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        if (session.RegistrationApplicationSession.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await _submissionService.SubmitRegistrationApplicationAsync(
                session.RegistrationApplicationSession.SubmissionId.Value,
                session.RegistrationSession.SelectedComplianceScheme?.Id, null, "PayByBankTransfer",
                session.RegistrationApplicationSession.ApplicationReferenceNumber!,
                SubmissionType.RegistrationFeePayment);
        }

        return await RedirectToLandingPage(session);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RedirectFileUploadCompanyDetails)]
    public async Task<IActionResult> RedirectToFileUpload()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.RegistrationSession.Journey = new List<string> { PagePaths.FileUploadCompanyDetailsSubLanding };
        session.RegistrationSession.SubmissionPeriod = _period.DataPeriod;
        session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration = true;
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return await RedirectToRightAction(session);
    }

    [ExcludeFromCodeCoverage]
    private async Task<RedirectToActionResult> RedirectToRightAction(FrontendSchemeRegistrationSession session)
    {
        var submissions = await _submissionService.GetSubmissionsAsync<RegistrationSubmission>(
            new List<string> { _period.DataPeriod },
            1,
            session.RegistrationSession.SelectedComplianceScheme?.Id);

        var submission = submissions.FirstOrDefault();

        if (submission != null)
        {
            var submissionStatus = submission.GetSubmissionStatus();

            switch (submissionStatus)
            {
                case SubmissionPeriodStatus.FileUploaded
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                case SubmissionPeriodStatus.SubmittedToRegulator
                    when session.UserData.ServiceRole.Parse<ServiceRole>()
                        .In(ServiceRole.Delegated, ServiceRole.Approved):
                case SubmissionPeriodStatus.SubmittedToRegulator
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                case SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload
                    when session.UserData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.FileUploaded when session.UserData.ServiceRole.Parse<ServiceRole>()
                    .In(ServiceRole.Delegated, ServiceRole.Approved):
                case SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload
                    when session.UserData.ServiceRole.Parse<ServiceRole>()
                        .In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(ReviewCompanyDetailsController.Get),
                        nameof(ReviewCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", submission.Id } });
                case SubmissionPeriodStatus.NotStarted:
                    return RedirectToAction(
                        nameof(FileUploadCompanyDetailsController.Get),
                        nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(),
                        new { dataPeriod = _period.DataPeriod });
            }
        }

        return RedirectToAction("Get", "FileUploadCompanyDetails", new { dataPeriod = _period.DataPeriod });
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

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        ViewBag.BackLinkToDisplay = session.RegistrationSession.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
    }

    private async Task SetOrReplaceBackLink(FrontendSchemeRegistrationSession session, string currentPagePath, string pagePathToReplace, string pagePathToReplaceWith)
    {
        if (!string.IsNullOrEmpty(pagePathToReplace) && !string.IsNullOrEmpty(pagePathToReplaceWith))
        {
            var index = session.RegistrationSession.Journey.IndexOf(pagePathToReplace);
            session.RegistrationSession.Journey[index] = pagePathToReplaceWith;
            await SaveSession(session, currentPagePath, null);
        }

        SetBackLink(session, currentPagePath);
    }

    private async Task<List<ComplianceSchemeDto>> GetComplianceSchemes()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var complianceSchemes = await _complianceSchemeService.GetComplianceSchemes();
        var complianceSchemesList = complianceSchemes.OrderBy(x => x.Name).ToList();
        if (session.RegistrationSession.IsUpdateJourney)
        {
            var currentComplianceScheme = session.RegistrationSession.CurrentComplianceScheme;
            complianceSchemesList.Remove(
                complianceSchemesList.SingleOrDefault(x => x.Id == currentComplianceScheme.ComplianceSchemeId));
        }

        return complianceSchemesList;
    }

    private async Task<string> InitiatePayment(FrontendSchemeRegistrationSession session)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations.First();

        var request = new PaymentInitiationRequest
        {
            UserId = userData.Id!.Value,
            OrganisationId = organisation.Id!.Value,
            Reference = session.RegistrationApplicationSession.ApplicationReferenceNumber!,
            Description = "Registration fee",
            Regulator = await _paymentCalculationService.GetRegulatorNation(organisation.Id.Value),
            Amount = session.RegistrationApplicationSession.TotalAmountOutstanding
        };
        return await _paymentCalculationService.InitiatePayment(request);
    }

    private async Task<RedirectToActionResult> RedirectToLandingPage(FrontendSchemeRegistrationSession session)
    {
        var isComplianceScheme = session.RegistrationSession.SelectedComplianceScheme != null;

        if (isComplianceScheme)
        {
            return RedirectToAction(
                nameof(ComplianceSchemeLandingController.Get),
                nameof(ComplianceSchemeLandingController).RemoveControllerFromName());
        }
        else
        {
            return RedirectToAction(nameof(VisitHomePageSelfManaged));
        }
    }
}