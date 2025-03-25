﻿using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.ResubmissionApplication;

[FeatureGate(FeatureFlags.ImplementPackagingDataResubmissionJourney)]
[Route("packaging-resubmission")]
public class ResubmissionApplicationController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IResubmissionApplicationService _resubmissionApplicationService;
    private readonly IUserAccountService _userAccountService;

    public ResubmissionApplicationController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IResubmissionApplicationService resubmissionApplicationService,
        IUserAccountService userAccountService)
    {
        _resubmissionApplicationService = resubmissionApplicationService;
        _sessionManager = sessionManager;
        _userAccountService = userAccountService;
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [$"/report-data/{PagePaths.ResubmissionFeeCalculations}", PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions);

        var submissions = session.PomResubmissionSession.PomSubmissions;
        var submission = submissions.FirstOrDefault();

        var model = new SelectPaymentOptionsViewModel
        {
            RegulatorNation = session.PomResubmissionSession.RegulatorNation,
            TotalAmountOutstanding = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding)
        };

        if (!model.IsEngland)
        {
            await _resubmissionApplicationService.CreatePackagingDataResubmissionFeePaymentEvent(
            session.PomResubmissionSession.PackagingResubmissionApplicationSession.SubmissionId,
            submission?.LastSubmittedFile.FileId,
            Enum.GetName(typeof(PaymentOptions), 2));

            return RedirectToAction(nameof(PayByBankTransfer));
        }
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.SelectPaymentOptions)]
    public async Task<IActionResult> SelectPaymentOptions(SelectPaymentOptionsViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [$"/report-data/{PagePaths.ResubmissionFeeCalculations}", PagePaths.SelectPaymentOptions];
        SetBackLink(session, PagePaths.SelectPaymentOptions);

        var submissions = session.PomResubmissionSession.PomSubmissions;
        var submission = submissions.FirstOrDefault();

        model.RegulatorNation = session.PomResubmissionSession.RegulatorNation;
        model.TotalAmountOutstanding = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _resubmissionApplicationService.CreatePackagingDataResubmissionFeePaymentEvent(
            session.PomResubmissionSession.PackagingResubmissionApplicationSession.SubmissionId,
            submission?.LastSubmittedFile.FileId,
           Enum.GetName(typeof(PaymentOptions), model.PaymentOption));

        switch (model.PaymentOption)
        {
            case (int)PaymentOptions.PayOnline:
                return (!model.IsEngland) ? RedirectToAction(nameof(PayByBankTransfer)) : RedirectToAction(nameof(PayOnline));
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
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone];
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone);

        var model = new PaymentOptionPayByPhoneViewModel()
        {
            TotalAmountOutstanding = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding),
            ApplicationReferenceNumber = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber,
        };

        return View("PaymentOptionPayByPhone", model);
    }


    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayOnline)]
    public async Task<IActionResult> PayOnline()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByPhone];
        SetBackLink(session, PagePaths.PaymentOptionPayByPhone);

        var paymentLink = await _resubmissionApplicationService.InitiatePayment(User, HttpContext.Session);
        var model = new PaymentOptionPayOnlineViewModel()
        {
            TotalAmountOutstanding = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding),
            ApplicationReferenceNumber = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber,
            PaymentLink = paymentLink
        };

        return View("PaymentOptionPayOnline", model);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.PaymentOptionPayByBankTransfer)]
    public async Task<IActionResult> PayByBankTransfer()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer];
        
        if(NationExtensions.GetNationName(session.PomResubmissionSession.RegulatorNation) != Nation.England.ToString())
        {
            ViewBag.BackLinkToDisplay = Url.Content($"/report-data/{PagePaths.ResubmissionFeeCalculations}");            
        }
        else
        {            
            SetBackLink(session, PagePaths.PaymentOptionPayByBankTransfer);
        }
       

        var model = new PaymentOptionPayByBankTransferViewModel()
        {
            TotalAmountOutstanding = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding),
            ApplicationReferenceNumber = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber,
            RegulatorNation = session.PomResubmissionSession.RegulatorNation
        };

        return View("PaymentOptionPayByBankTransfer", model);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [$"/report-data/{PagePaths.ResubmissionTaskList}", $"packaging-resubmission/{PagePaths.AdditionalInformation}"];
        SetBackLink(session, $"packaging-resubmission/{PagePaths.AdditionalInformation}");

        return View(new AdditionalInformationViewModel
        {
            RegulatorNation = session.PomResubmissionSession.RegulatorNation,
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            IsApprovedOrDelegatedUser = userData.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson
        });
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.AdditionalInformation)]
    public async Task<IActionResult> AdditionalInformation(AdditionalInformationViewModel model)
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = [$"/report-data/{PagePaths.ResubmissionTaskList}", $"packaging-resubmission/{PagePaths.AdditionalInformation}"];
        SetBackLink(session, $"packaging-resubmission/{PagePaths.AdditionalInformation}");

        var submissions = session.PomResubmissionSession.PomSubmissions;
        var submission = submissions.FirstOrDefault();
        var submittedByName = "";
        if (submission != null)
        {
            submittedByName = await GetUserNameFromId(submission.LastSubmittedFile.SubmittedBy!);
        }

        await _resubmissionApplicationService.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(
            session.PomResubmissionSession.PackagingResubmissionApplicationSession.SubmissionId,
            submission?.LastSubmittedFile.FileId, submittedByName,
            DateTime.Now, model?.AdditionalInformationText);

        return RedirectToAction(nameof(SubmitToEnvironmentRegulator));
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprSelectSchemePolicy)]
    [Route(PagePaths.SubmitToEnvironmentRegulator)]
    public async Task<IActionResult> SubmitToEnvironmentRegulator()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();

        return View("ResubmissionConfirmation",
            new ApplicationSubmissionConfirmationViewModel
            {
                RegistrationApplicationSubmittedDate = session.PomResubmissionSession.PomSubmissions.FirstOrDefault()?.LastSubmittedFile.SubmittedDateTime,
                RegistrationReferenceNumber = session.PomResubmissionSession.PomResubmissionReferences.FirstOrDefault().Value,
                ApplicationStatus = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationStatus
            }
        );
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        ViewBag.BackLinkToDisplay = session.PomResubmissionSession.Journey.PreviousOrDefault(currentPagePath) ?? string.Empty;
    }

    private async Task<string> GetUserNameFromId(Guid userId)
    {
        var user = await _userAccountService.GetPersonByUserId(userId);
        return $"{user.FirstName} {user.LastName}";
    }
}