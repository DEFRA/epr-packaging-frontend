using System.Web;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.Error;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Polly.Caching;

namespace FrontendSchemeRegistration.UI.Controllers.RegistrationApplication;

public class RegistrationApplicationController(
    ISessionManager<RegistrationApplicationSession> sessionManager,
    ILogger<RegistrationApplicationController> logger,
    IRegistrationApplicationService registrationApplicationService
)
    : Controller
{
    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.ProducerRegistrationGuidance)]
    public async Task<IActionResult> ProducerRegistrationGuidance()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isResubmission = !string.IsNullOrWhiteSpace(HttpContext.Request.Query["IsResubmission"]);

        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);
        var session = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), isResubmission);    
        session.Journey = [session.IsComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.ProducerRegistrationGuidance];


        if (session.ApplicationStatus is
                ApplicationStatusType.FileUploaded
                or ApplicationStatusType.SubmittedAndHasRecentFileUpload
                or ApplicationStatusType.CancelledByRegulator
                or ApplicationStatusType.QueriedByRegulator
                or ApplicationStatusType.RejectedByRegulator
            || session.FileUploadStatus is
                RegistrationTaskListStatus.Pending
                or RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new {registrationyear = registrationYear});
        }

        return View(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            RegistrationYear = registrationYear.GetValueOrDefault(),
            IsComplianceScheme = userData.Organisations[0].OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.SelectedComplianceScheme?.Name!
        });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationTaskList)]
    public async Task<IActionResult> RegistrationTaskList()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isResubmission = !string.IsNullOrWhiteSpace(HttpContext.Request.Query["IsResubmission"]);
        RegistrationJourney? registrationJourney = null;
        if (Enum.TryParse<RegistrationJourney>(HttpContext.Request.Query["registrationjourney"].ToString(), true, out var registrationJourneyResult))
        {
            registrationJourney = registrationJourneyResult;
        }
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, registrationYear.GetValueOrDefault(), isResubmission, registrationJourney);
        session.Journey = [session.IsComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.RegistrationTaskList];

        SetBackLink(session, PagePaths.RegistrationTaskList, registrationYear, registrationJourney);

        return View(new RegistrationTaskListViewModel
        {
            IsResubmission = session.IsResubmission,
            OrganisationName = organisation.Name,
            IsComplianceScheme = session.IsComplianceScheme,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            ApplicationStatus = session.ApplicationStatus,
            FileUploadStatus = session.FileUploadStatus,
            PaymentViewStatus = session.PaymentViewStatus,
            AdditionalDetailsStatus = session.AdditionalDetailsStatus,
            RegistrationYear = registrationYear.GetValueOrDefault(),
            ShowRegistrationCaption = session.ShowRegistrationCaption,
            RegistrationJourney = session.RegistrationJourney
        });
    }
    
    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationFeeCalculations)]
    public async Task<IActionResult> RegistrationFeeCalculations([FromQuery]RegistrationJourney? registrationJourney)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationTaskList, PagePaths.RegistrationFeeCalculations];
        SetBackLink(session, PagePaths.RegistrationFeeCalculations, registrationYear, registrationJourney);

        if (session.FileUploadStatus is not RegistrationTaskListStatus.Completed)
        {
            logger.LogWarning("RegistrationApplicationSession.FileUploadStatus is not Completed for ApplicationReferenceNumber {Number}", session.ApplicationReferenceNumber);
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
        }

        logger.LogInformation("getting Registration Fee Details for OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", organisation.OrganisationNumber!, session.ApplicationReferenceNumber, session.SelectedComplianceScheme?.Id);

        if (session.IsComplianceScheme)
        {
            var response = await registrationApplicationService.GetComplianceSchemeRegistrationFees(HttpContext.Session);

            if (response is not null)
            {
                response.RegistrationYear = registrationYear.GetValueOrDefault();
                response.RegistrationJourney = registrationJourney;
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

        logger.LogWarning("Error in Getting Registration Fees Details for SubmissionId {SubmissionId}, OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", session.SubmissionId, organisation.OrganisationNumber!, session.ApplicationReferenceNumber, session.SelectedComplianceScheme?.Id);

        return RedirectToAction(
            nameof(ErrorController.HandleThrownExceptions),
            nameof(ErrorController).RemoveControllerFromName());
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions()
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions, registrationYear);

        var model = new SelectPaymentOptionsViewModel
        {
            RegulatorNation = session.RegulatorNation,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            RegistrationYear = registrationYear.GetValueOrDefault()
        };

        if (!model.IsEngland)
        {
            return RedirectToAction(nameof(PayByBankTransfer), new { registrationyear = registrationYear });
        }

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions(SelectPaymentOptionsViewModel model)
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        SetBackLink(session, PagePaths.SelectPaymentOptions, registrationYear);

        model.RegulatorNation = session.RegulatorNation;
        model.TotalAmountOutstanding = session.TotalAmountOutstanding;
        model.RegistrationYear = registrationYear.GetValueOrDefault();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (model.PaymentOption)
        {
            case (int) PaymentOptions.PayOnline:
                return RedirectToAction(nameof(PayOnline), new { registrationyear = registrationYear });
            case (int) PaymentOptions.PayByBankTransfer:
                return RedirectToAction(nameof(PayByBankTransfer), new { registrationyear = registrationYear });
            case (int) PaymentOptions.PayByPhone:
                return RedirectToAction(nameof(PayByPhone), new { registrationyear = registrationYear });
            default: return View(model);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByPhone)]
    public async Task<IActionResult> PayByPhone()
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone];
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone, registrationYear);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
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
                ApplicationReferenceNumber = session.ApplicationReferenceNumber!

            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnline()
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayOnline];
        SetBackLink(session, PagePaths.PaymentOptionPayOnline, registrationYear);

        var paymentLink = await registrationApplicationService.InitiatePayment(User, HttpContext.Session);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
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
                RegistrationYear = registrationYear.GetValueOrDefault()
            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransfer()
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer];

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
        }

        var model = new PaymentOptionPayByBankTransferViewModel
        {
            IsComplianceScheme = session.IsComplianceScheme,
            RegulatorNation = session.RegulatorNation,
            ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            RegistrationYear = registrationYear.GetValueOrDefault()
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
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationTaskList, PagePaths.AdditionalInformation];
        SetBackLink(session, PagePaths.AdditionalInformation, registrationYear);

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
            return RedirectToAction(nameof(RegistrationTaskList), new { registrationyear = registrationYear });
        }

        return View(new AdditionalInformationViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.SelectedComplianceScheme?.Name!,
            IsResubmission = session.IsResubmission,
            RegistrationYear = registrationYear.GetValueOrDefault()
        });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation(AdditionalInformationViewModel model)
    {
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

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
        var session = await sessionManager.GetSessionAsync(HttpContext.Session);
        session.Journey = [session.IsComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.UpdateRegistrationGuidance];

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
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();

        if (session.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            session.Journey = [PagePaths.RegistrationTaskList, PagePaths.SubmitRegistrationRequest];
            SetBackLink(session, PagePaths.SubmitRegistrationRequest, registrationYear);
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
                RegistrationYear = registrationYear.GetValueOrDefault()
            }
        );
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RedirectFileUploadCompanyDetails)]
    public async Task<IActionResult> RedirectToFileUpload(RegistrationJourney? registrationJourney = null)
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var registrationYear = registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"],false);

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
                        new RouteValueDictionary { { "submissionId", session.SubmissionId }, { "registrationyear", registrationYear } });
                case ApplicationStatusType.FileUploaded
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved):
                case ApplicationStatusType.SubmittedAndHasRecentFileUpload
                    when userData.ServiceRole.Parse<ServiceRole>().In(ServiceRole.Delegated, ServiceRole.Approved):
                    return RedirectToAction(
                        nameof(ReviewCompanyDetailsController.Get),
                        nameof(ReviewCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", session.SubmissionId }, { "registrationyear", registrationYear }, { "registrationjourney", registrationJourney } });
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

    private void SetBackLink(RegistrationApplicationSession session, string currentPagePath, int? registrationYear = null, RegistrationJourney? registrationJourney = null)
    {
        var previousPage = session.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
        if(registrationYear > 0 && !string.IsNullOrWhiteSpace(previousPage))
        {
            previousPage = QueryHelpers.AddQueryString(previousPage, "registrationyear", registrationYear.ToString());            
        }

        if (registrationJourney.HasValue && !string.IsNullOrWhiteSpace(previousPage))
        {
            previousPage = QueryHelpers.AddQueryString(previousPage, "registrationjourney", registrationJourney.ToString());
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