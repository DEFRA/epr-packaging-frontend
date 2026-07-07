using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.Error;
using FrontendSchemeRegistration.UI.Domain;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Controllers.RegistrationApplication;

[RegistrationApplicationSessionLoggingScopeActionFilter]
public class RegistrationApplicationController(
    ISessionManager<RegistrationApplicationSession> sessionManager,
    ILogger<RegistrationApplicationController> logger,
    IRegistrationApplicationService registrationApplicationService,
    IRegistrationPeriodProvider registrationPeriodProvider,
    IFeatureManager featureManager,
    IRegistrationFactory registrationFactory,
    IPaymentCalculationService paymentCalculationService
)
    : Controller
{
    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.ProducerRegistrationGuidance)]
    public async Task<IActionResult> ProducerRegistrationGuidance([FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isResubmission = !string.IsNullOrWhiteSpace(HttpContext.Request.Query["IsResubmission"]);

        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);

        var session = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), registrationJourney, isResubmission);

        if (session.SkipProducerRegistrationGuidance)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationjourney = registrationJourney });
        }

        session.Journey = [session.IsComplianceScheme ? PagePaths.CsoRegistration : PagePaths.HomePageSelfManaged, PagePaths.ProducerRegistrationGuidance];

        var nation = NationExtensions.GetNationName(session.RegulatorNation);
        SetBackLink(session, PagePaths.ProducerRegistrationGuidance, null, null, session.IsComplianceScheme ? nation : null);

        return View(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            RegistrationYear = registrationYear.GetValueOrDefault(),
            IsComplianceScheme = userData.Organisations[0].OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.SelectedComplianceScheme?.Name!,
            RegistrationJourney = registrationJourney,
            ShowRegistrationCaption = session.ShowRegistrationCaption
        });
    }


    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationTaskList)]
    public async Task<IActionResult> RegistrationTaskList([FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isResubmission = !string.IsNullOrWhiteSpace(HttpContext.Request.Query["IsResubmission"]);

        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);

        if (await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationDomainModel))
        {
            var registration = await registrationFactory.CreateAsync(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), registrationJourney, isResubmission);

            return View("RegistrationTaskListDomain", new RegistrationTaskListDomainViewModel(
                registration,
                organisation.Name,
                organisation.OrganisationNumber.ToReferenceNumberFormat(),
                registrationYear.GetValueOrDefault()));
        }

        var legacySession = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), registrationJourney, isResubmission);
        legacySession.Journey = [legacySession.IsComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.RegistrationTaskList];

        if (legacySession.SkipProducerRegistrationGuidance)
        {
            legacySession.Journey = [legacySession.IsComplianceScheme ? PagePaths.CsoRegistration : PagePaths.HomePageSelfManaged, PagePaths.RegistrationTaskList];
            var nation = NationExtensions.GetNationName(legacySession.RegulatorNation);
            SetBackLink(legacySession, PagePaths.RegistrationTaskList, null, null, legacySession.IsComplianceScheme ? nation : null);
        }
        else
        {
            legacySession.Journey = [PagePaths.ProducerRegistrationGuidance, PagePaths.RegistrationTaskList];
            SetBackLink(legacySession, PagePaths.RegistrationTaskList, registrationYear, registrationJourney);
        }

        return View(new RegistrationTaskListViewModel
        {
            IsResubmission = legacySession.IsResubmission,
            OrganisationName = organisation.Name,
            IsComplianceScheme = legacySession.IsComplianceScheme,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            ApplicationStatus = legacySession.ApplicationStatus,
            FileUploadStatus = legacySession.FileUploadStatus,
            PaymentViewStatus = legacySession.PaymentViewStatus,
            AdditionalDetailsStatus = legacySession.AdditionalDetailsStatus,
            RegistrationYear = registrationYear.GetValueOrDefault(),
            ShowRegistrationCaption = legacySession.ShowRegistrationCaption,
            RegistrationJourney = legacySession.RegistrationJourney
        });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationFeeCalculations)]
    public async Task<IActionResult> RegistrationFeeCalculations()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        if (await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationDomainModel))
        {
            var registration = await registrationFactory.CreateAsync(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), null);

            if (!registration.CanViewFeeCalculations)
            {
                logger.LogWarning("RegistrationApplicationSession.FileUploadStatus is not Completed for ApplicationReferenceNumber {Number}", registration.ApplicationReferenceNumber);
                return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
            }

            logger.LogInformation("getting Registration Fee Details for OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", organisation.OrganisationNumber!, registration.ApplicationReferenceNumber, registration.SelectedComplianceSchemeId);

            if (registration.IsComplianceScheme)
            {
                var response = await registration.GetComplianceSchemeRegistrationFees(paymentCalculationService, logger);

                if (response is not null)
                {
                    response.RegistrationYear = registrationYear.GetValueOrDefault();
                    response.RegistrationJourney = registration.RegistrationJourney;
                    return View("ComplianceSchemeRegistrationFeeCalculationsDomain", response);
                }
            }
            else
            {
                var response = await registration.GetProducerRegistrationFees(paymentCalculationService, logger);

                if (response is not null)
                {
                    response.RegistrationYear = registrationYear.GetValueOrDefault();
                    return View("RegistrationFeeCalculationsDomain", response);
                }
            }

            logger.LogWarning("Error in Getting Registration Fees Details for OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", organisation.OrganisationNumber!, registration.ApplicationReferenceNumber, registration.SelectedComplianceSchemeId);

            return RedirectToAction(
                nameof(ErrorController.HandleThrownExceptions),
                nameof(ErrorController).RemoveControllerFromName());
        }

        var legacySession = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        legacySession.Journey = [PagePaths.RegistrationTaskList, PagePaths.RegistrationFeeCalculations];
        SetBackLink(legacySession, PagePaths.RegistrationFeeCalculations, registrationYear, legacySession.RegistrationJourney);

        if (legacySession.FileUploadStatus is not RegistrationTaskListStatus.Completed)
        {
            logger.LogWarning("RegistrationApplicationSession.FileUploadStatus is not Completed for ApplicationReferenceNumber {Number}", legacySession.ApplicationReferenceNumber);
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
        }

        logger.LogInformation("getting Registration Fee Details for OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", organisation.OrganisationNumber!, legacySession.ApplicationReferenceNumber, legacySession.SelectedComplianceScheme?.Id);

        if (legacySession.IsComplianceScheme)
        {
            var response = await registrationApplicationService.GetComplianceSchemeRegistrationFees(HttpContext.Session);

            if (response is not null)
            {
                response.RegistrationYear = registrationYear.GetValueOrDefault();
                response.RegistrationJourney = legacySession.RegistrationJourney;
                return View("ComplianceSchemeRegistrationFeeCalculations", response);
            }
        }
        else
        {
            var response = await registrationApplicationService.GetProducerRegistrationFees(HttpContext.Session);

            if (response is not null)
            {
                response.RegistrationYear = registrationYear.GetValueOrDefault();
                return View(response);
            }
        }

        logger.LogWarning("Error in Getting Registration Fees Details for SubmissionId {SubmissionId}, OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", legacySession.SubmissionId, organisation.OrganisationNumber!, legacySession.ApplicationReferenceNumber, legacySession.SelectedComplianceScheme?.Id);

        return RedirectToAction(
            nameof(ErrorController.HandleThrownExceptions),
            nameof(ErrorController).RemoveControllerFromName());
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions, registrationYear, session.RegistrationJourney);

        var model = new SelectPaymentOptionsViewModel
        {
            RegulatorNation = session.RegulatorNation,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            RegistrationYear = registrationYear.GetValueOrDefault(),
            ShowRegistrationCaption = session.ShowRegistrationCaption,
            RegistrationJourney = session.RegistrationJourney,
            IsComplianceScheme = session.IsComplianceScheme,
            OrganisationName = organisation.Name!
        };

        if (!model.IsEngland)
        {
            return RedirectToAction(nameof(PayByBankTransfer), new { registrationyear = registrationYear });
        }

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationJourney = session.RegistrationJourney });
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions(SelectPaymentOptionsViewModel model)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions, registrationYear);

        model.RegulatorNation = session.RegulatorNation;
        model.TotalAmountOutstanding = session.TotalAmountOutstanding;
        model.RegistrationYear = registrationYear.GetValueOrDefault();
        model.ShowRegistrationCaption = session.ShowRegistrationCaption;
        model.RegistrationJourney = session.RegistrationJourney;
        model.IsComplianceScheme = session.IsComplianceScheme;
        model.OrganisationName = organisation.Name!;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (model.PaymentOption)
        {
            case (int)PaymentOptions.PayOnline:
                return RedirectToAction(nameof(PayOnline), new { registrationyear = registrationYear });
            case (int)PaymentOptions.PayByBankTransfer:
                return RedirectToAction(nameof(PayByBankTransfer), new { registrationyear = registrationYear });
            case (int)PaymentOptions.PayByPhone:
                return RedirectToAction(nameof(PayByPhone), new { registrationyear = registrationYear });
            default: return View(model);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByPhone)]
    public async Task<IActionResult> PayByPhone()
    {
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone];
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone, registrationYear);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationJourney = session.RegistrationJourney });
        }

        if (session.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await registrationApplicationService.CreateRegistrationApplicationEvent(HttpContext.Session, null, "PayByPhone", SubmissionType.RegistrationFeePayment);
        }

        return View("PaymentOptionPayByPhone",
            new PaymentOptionPayByPhoneViewModel
            {
                IsComplianceScheme = session.IsComplianceScheme,
                TotalAmountOutstanding = session.TotalAmountOutstanding,
                RegistrationYear = registrationYear.GetValueOrDefault(),
                ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
                RegistrationJourney = session.RegistrationJourney
            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnline()
    {
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayOnline];
        SetBackLink(session, PagePaths.PaymentOptionPayOnline, registrationYear);

        var paymentLink = await registrationApplicationService.InitiatePayment(User, HttpContext.Session);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationJourney = session.RegistrationJourney });
        }

        if (string.IsNullOrWhiteSpace(paymentLink))
        {
            return RedirectToAction(
                nameof(ErrorController.HandleThrownExceptions),
                nameof(ErrorController).RemoveControllerFromName());
        }
        
        if (session.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await registrationApplicationService.CreateRegistrationApplicationEvent(HttpContext.Session, null, "PayOnline", SubmissionType.RegistrationFeePayment);
        }

        return View("PaymentOptionPayOnline",
            new PaymentOptionPayOnlineViewModel
            {
                IsComplianceScheme = session.IsComplianceScheme,
                TotalAmountOutstanding = session.TotalAmountOutstanding,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
                PaymentLink = paymentLink,
                RegistrationYear = registrationYear.GetValueOrDefault(),
                RegistrationJourney = session.RegistrationJourney
            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransfer()
    {
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer];

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationJourney = session.RegistrationJourney });
        }

        var model = new PaymentOptionPayByBankTransferViewModel
        {
            IsComplianceScheme = session.IsComplianceScheme,
            RegulatorNation = session.RegulatorNation,
            ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            RegistrationYear = registrationYear.GetValueOrDefault(),
            RegistrationJourney = session.RegistrationJourney
        };

        if (!model.IsEngland)
        {
            await SetOrReplaceBackLink(session, PagePaths.PaymentOptionPayByBankTransfer, PagePaths.SelectPaymentOptions, PagePaths.RegistrationFeeCalculations, registrationYear);
        }
        else
        {
            SetBackLink(session, PagePaths.PaymentOptionPayByBankTransfer, registrationYear);
        }

        await SaveSession(session, PagePaths.PaymentOptionPayByBankTransfer, null);

        if (session.PaymentViewStatus != RegistrationTaskListStatus.Completed)
        {
            await registrationApplicationService.CreateRegistrationApplicationEvent(HttpContext.Session, null, "PayByBankTransfer", SubmissionType.RegistrationFeePayment);
        }

        return View("PaymentOptionPayByBankTransfer", model);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        
        session.Journey = [PagePaths.RegistrationTaskList, PagePaths.AdditionalInformation];
        SetBackLink(session, PagePaths.AdditionalInformation, registrationYear, session.RegistrationJourney);

        if (session is
            {
                AdditionalDetailsStatus: RegistrationTaskListStatus.Completed,
                ApplicationStatus: ApplicationStatusType.AcceptedByRegulator or
                ApplicationStatusType.ApprovedByRegulator or
                ApplicationStatusType.SubmittedToRegulator
            })
        {
            return RedirectToAction(nameof(SubmitRegistrationRequest), new { registrationyear = registrationYear });
        }

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            session.PaymentViewStatus != RegistrationTaskListStatus.Completed ||
            session.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear, registrationJourney = session.RegistrationJourney });
        }

        return View(new AdditionalInformationViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.SelectedComplianceScheme?.Name!,
            IsResubmission = session.IsResubmission,
            RegistrationYear = registrationYear.GetValueOrDefault(),
            RegistrationJourney = session.RegistrationJourney
        });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation(AdditionalInformationViewModel model)
    {
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();

        var isAuthorisedUser = User.GetUserData().ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved);
        if (!isAuthorisedUser)
        {
            return RedirectToAction(nameof(UnauthorisedUserWarnings));
        }

        if (session is { RegistrationApplicationSubmitted: false, FileUploadStatus: RegistrationTaskListStatus.Completed, PaymentViewStatus: RegistrationTaskListStatus.Completed })
        {
            await registrationApplicationService.CreateRegistrationApplicationEvent(HttpContext.Session, model.AdditionalInformationText, null, SubmissionType.RegistrationApplicationSubmitted);
        }

        return RedirectToAction(nameof(SubmitRegistrationRequest), new { registrationyear = registrationYear });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.UpdateRegistrationGuidance)]
    public async Task<IActionResult> UpdateRegistrationGuidance()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
        
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [isComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.UpdateRegistrationGuidance];

        await SaveSession(session, PagePaths.UpdateRegistrationGuidance, null);
        SetBackLink(session, PagePaths.UpdateRegistrationGuidance);

        return View();
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.UnauthorisedUserWarnings)]
    public async Task<IActionResult> UnauthorisedUserWarnings()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.AdditionalInformation, PagePaths.UnauthorisedUserWarnings];
        SetBackLink(session, PagePaths.UnauthorisedUserWarnings);

        ViewBag.IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        if (ViewBag.IsComplianceScheme)
        {
            ViewBag.ComplianceScheme = session.SelectedComplianceScheme?.Name;
            ViewBag.NationName = NationExtensions.GetNationName(session.RegulatorNation).ToLower();
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
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();

        if (session.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            session.Journey = [PagePaths.RegistrationTaskList, PagePaths.SubmitRegistrationRequest];
        }

        return View("ApplicationSubmissionConfirmation",
            new ApplicationSubmissionConfirmationViewModel
            {
                IsComplianceScheme = session.IsComplianceScheme,
                RegulatorNation = session.RegulatorNation,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
                RegistrationApplicationSubmittedDate = session.RegistrationApplicationSubmittedDate.Value,
                RegistrationReferenceNumber = session.RegistrationReferenceNumber!,
                ApplicationStatus = session.ApplicationStatus,
                isResubmission = session.IsResubmission,
                RegistrationYear = registrationYear.GetValueOrDefault(),
                RegistrationJourney = session.RegistrationJourney
            }
        );
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RedirectFileUploadCompanyDetails)]
    public async Task<IActionResult> RedirectToFileUpload()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.FileUploadCompanyDetailsSubLanding];
        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
        await registrationApplicationService.SetRegistrationFileUploadSession(HttpContext.Session, organisation.OrganisationNumber, registrationYear.GetValueOrDefault(), session.IsResubmission);
        
        if (session.SubmissionId != null)
        {
            switch (session.ApplicationStatus)
            {
                case ApplicationStatusType.FileUploaded
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                case ApplicationStatusType.SubmittedToRegulator
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved):
                case ApplicationStatusType.SubmittedToRegulator
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                case ApplicationStatusType.SubmittedAndHasRecentFileUpload
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Basic):
                    return RedirectToAction(
                        nameof(FileReUploadCompanyDetailsConfirmationController.Get),
                        nameof(FileReUploadCompanyDetailsConfirmationController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", session.SubmissionId } });
                case ApplicationStatusType.FileUploaded
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved):
                case ApplicationStatusType.SubmittedAndHasRecentFileUpload
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(ReviewCompanyDetailsController.Get),
                        nameof(ReviewCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", session.SubmissionId }, { "registrationyear", registrationYear }, { "registrationjourney", session.RegistrationJourney } });
                case ApplicationStatusType.NotStarted:
                case ApplicationStatusType.QueriedByRegulator:
                case ApplicationStatusType.CancelledByRegulator:
                case ApplicationStatusType.RejectedByRegulator:
                    return RedirectToAction(
                        nameof(FileUploadCompanyDetailsController.Get),
                        nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", session.SubmissionId }, { "dataPeriod", session.Period.DataPeriod }, { "registrationyear", registrationYear },{"registrationjourney", session.RegistrationJourney} });
            }
        }

        return RedirectToAction(nameof(FileUploadCompanyDetailsController.Get), nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(), new RouteValueDictionary { { "dataPeriod", session.Period.DataPeriod }, { "registrationyear", registrationYear }, {"registrationjourney", session.RegistrationJourney} });
    }
        
    private static void ClearRestOfJourney(RegistrationApplicationSession session, string currentPagePath)
    {
        var index = session.Journey.IndexOf(currentPagePath);

        // this also cover if current page not found (index = -1) then it clears all pages
        session.Journey = session.Journey.Take(index + 1).ToList();
    }

    private async Task SaveSession(RegistrationApplicationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.Journey.AddIfNotExists(nextPagePath);

        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private void SetBackLink(RegistrationApplicationSession session, string currentPagePath, int? registrationYear = null, RegistrationJourney? registrationJourney = null, string? nation = null)
    {
        var previousPage = session.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
        if (registrationYear > 0 && !string.IsNullOrWhiteSpace(previousPage))
        {
            previousPage = QueryHelpers.AddQueryString(previousPage, "registrationyear", registrationYear.ToString());
        }

        if (registrationJourney.HasValue && !string.IsNullOrWhiteSpace(previousPage))
        {
            previousPage = QueryHelpers.AddQueryString(previousPage, "registrationjourney", registrationJourney.ToString());
        }

        if (nation is not null && !string.IsNullOrWhiteSpace(previousPage))
        {
            previousPage = QueryHelpers.AddQueryString(previousPage, "nation", nation.ToString());
        }

        ViewBag.BackLinkToDisplay = previousPage;
    }

    private async Task SetOrReplaceBackLink(RegistrationApplicationSession session, string currentPagePath, string pagePathToReplace, string pagePathToReplaceWith, int? registrationYear = null, RegistrationJourney? registrationjourney = null)
    {
        if (!string.IsNullOrEmpty(pagePathToReplace) && !string.IsNullOrEmpty(pagePathToReplaceWith))
        {
            var index = session.Journey.IndexOf(pagePathToReplace);
            session.Journey[index] = pagePathToReplaceWith;
            await SaveSession(session, currentPagePath, null);
        }

        SetBackLink(session, currentPagePath, registrationYear, registrationjourney);
    }
}