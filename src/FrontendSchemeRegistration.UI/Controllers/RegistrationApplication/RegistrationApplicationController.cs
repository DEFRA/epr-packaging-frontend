﻿using EPR.Common.Authorization.Constants;
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
        
        var session = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, isResubmission);
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
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        return View(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
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
        
        var session = await registrationApplicationService.GetRegistrationApplicationSession(HttpContext.Session, organisation, isResubmission);
        session.Journey = [session.IsComplianceScheme ? PagePaths.ComplianceSchemeLanding : PagePaths.HomePageSelfManaged, PagePaths.RegistrationTaskList];

        SetBackLink(session, PagePaths.RegistrationTaskList);

        return View(new RegistrationTaskListViewModel
        {
            IsResubmission = session.IsResubmission,
            OrganisationName = organisation.Name!,
            IsComplianceScheme = session.IsComplianceScheme,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            ApplicationStatus = session.ApplicationStatus,
            FileUploadStatus = session.FileUploadStatus,
            PaymentViewStatus = session.PaymentViewStatus,
            AdditionalDetailsStatus = session.AdditionalDetailsStatus
        });
    }
    
    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RegistrationFeeCalculations)]
    public async Task<IActionResult> RegistrationFeeCalculations()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationTaskList, PagePaths.RegistrationFeeCalculations];
        SetBackLink(session, PagePaths.RegistrationFeeCalculations);

        if (session.FileUploadStatus is not RegistrationTaskListStatus.Completed)
        {
            logger.LogWarning("RegistrationApplicationSession.FileUploadStatus is not Completed for ApplicationReferenceNumber {Number}", session.ApplicationReferenceNumber);
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        logger.LogInformation("getting Registration Fee Details for OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", organisation.OrganisationNumber!, session.ApplicationReferenceNumber, session.SelectedComplianceScheme?.Id);

        if (session.IsComplianceScheme)
        {
            var response = await registrationApplicationService.GetComplianceSchemeRegistrationFees(HttpContext.Session);

            if (response is not null) return View("ComplianceSchemeRegistrationFeeCalculations", response);
        }
        else
        {
            var response = await registrationApplicationService.GetProducerRegistrationFees(HttpContext.Session);

            if (response is not null) return View(response);
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
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions);

        var model = new SelectPaymentOptionsViewModel
        {
            RegulatorNation = session.RegulatorNation,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
        };

        if (!model.IsEngland)
        {
            return RedirectToAction(nameof(PayByBankTransfer));
        }

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions(SelectPaymentOptionsViewModel model)
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        SetBackLink(session, PagePaths.SelectPaymentOptions);

        model.RegulatorNation = session.RegulatorNation;
        model.TotalAmountOutstanding = session.TotalAmountOutstanding;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (model.PaymentOption)
        {
            case (int) PaymentOptions.PayOnline:
                return RedirectToAction(nameof(PayOnline));
            case (int) PaymentOptions.PayByBankTransfer:
                return RedirectToAction(nameof(PayByBankTransfer));
            case (int) PaymentOptions.PayByPhone:
                return RedirectToAction(nameof(PayByPhone));
            default: return View(model);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByPhone)]
    public async Task<IActionResult> PayByPhone()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone];
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList));
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
                ApplicationReferenceNumber = session.ApplicationReferenceNumber!
            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnline()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayOnline];
        SetBackLink(session, PagePaths.PaymentOptionPayOnline);

        var paymentLink = await registrationApplicationService.InitiatePayment(User, HttpContext.Session);

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
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
                PaymentLink = paymentLink
            });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransfer()
    {
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer];

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            string.IsNullOrWhiteSpace(session.ApplicationReferenceNumber))
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        var model = new PaymentOptionPayByBankTransferViewModel
        {
            IsComplianceScheme = session.IsComplianceScheme,
            RegulatorNation = session.RegulatorNation,
            ApplicationReferenceNumber = session.ApplicationReferenceNumber!,
            TotalAmountOutstanding = session.TotalAmountOutstanding
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

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.RegistrationTaskList, PagePaths.AdditionalInformation];
        SetBackLink(session, PagePaths.AdditionalInformation);

        if (session is
            {
                AdditionalDetailsStatus: RegistrationTaskListStatus.Completed,
                ApplicationStatus: ApplicationStatusType.AcceptedByRegulator or
                ApplicationStatusType.ApprovedByRegulator or
                ApplicationStatusType.SubmittedToRegulator
            })
        {
            return RedirectToAction(nameof(SubmitRegistrationRequest));
        }

        if (session.FileUploadStatus != RegistrationTaskListStatus.Completed ||
            session.PaymentViewStatus != RegistrationTaskListStatus.Completed ||
            session.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            return RedirectToAction(nameof(RegistrationTaskList));
        }

        return View(new AdditionalInformationViewModel
        {
            RegulatorNation = session.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            ComplianceScheme = session.SelectedComplianceScheme?.Name!,
            IsResubmission = session.IsResubmission
        });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation(AdditionalInformationViewModel model)
    {
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

        return RedirectToAction(nameof(SubmitRegistrationRequest));
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
        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();

        if (session.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed)
        {
            session.Journey = [PagePaths.RegistrationTaskList, PagePaths.SubmitRegistrationRequest];
            SetBackLink(session, PagePaths.SubmitRegistrationRequest);
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
                isResubmission = session.IsResubmission
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

        var session = await sessionManager.GetSessionAsync(HttpContext.Session) ?? new RegistrationApplicationSession();
        session.Journey = [PagePaths.FileUploadCompanyDetailsSubLanding];
        await sessionManager.SaveSessionAsync(HttpContext.Session, session);
        await registrationApplicationService.SetRegistrationFileUploadSession(HttpContext.Session, organisation.OrganisationNumber, session.IsResubmission);
        
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
                        new RouteValueDictionary { { "submissionId", session.SubmissionId } });
                case ApplicationStatusType.NotStarted:
                case ApplicationStatusType.QueriedByRegulator:
                case ApplicationStatusType.CancelledByRegulator:
                case ApplicationStatusType.RejectedByRegulator:
                    return RedirectToAction(
                        nameof(FileUploadCompanyDetailsController.Get),
                        nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(),
                        new RouteValueDictionary { { "submissionId", session.SubmissionId }, { "dataPeriod", session.Period.DataPeriod } });
            }
        }

        return RedirectToAction(nameof(FileUploadCompanyDetailsController.Get), nameof(FileUploadCompanyDetailsController).RemoveControllerFromName(), new RouteValueDictionary { { "dataPeriod", session.Period.DataPeriod } });
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

    private void SetBackLink(RegistrationApplicationSession session, string currentPagePath)
    {
        ViewBag.BackLinkToDisplay = session.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
    }

    private async Task SetOrReplaceBackLink(RegistrationApplicationSession session, string currentPagePath, string pagePathToReplace, string pagePathToReplaceWith)
    {
        if (!string.IsNullOrEmpty(pagePathToReplace) && !string.IsNullOrEmpty(pagePathToReplaceWith))
        {
            var index = session.Journey.IndexOf(pagePathToReplace);
            session.Journey[index] = pagePathToReplaceWith;
            await SaveSession(session, currentPagePath, null);
        }

        SetBackLink(session, currentPagePath);
    }
}