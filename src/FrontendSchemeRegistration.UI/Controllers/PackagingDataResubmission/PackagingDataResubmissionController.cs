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
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers.FrontendSchemeRegistration;

[FeatureGate(FeatureFlags.ImplementPackagingDataResubmissionJourney)]
public class PackagingDataResubmissionController : Controller
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ILogger<PackagingDataResubmissionController> _logger;
    private readonly IUserAccountService _userAccountService;
    private readonly List<SubmissionPeriod> _submissionPeriods;
    private readonly IResubmissionApplicationService _resubmissionApplicationService;
    private readonly IComplianceSchemeService _complianceSchemeService;

    public PackagingDataResubmissionController(
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<PackagingDataResubmissionController> logger,
        IUserAccountService userAccountService,
        IOptions<GlobalVariables> globalVariables,
        IResubmissionApplicationService resubmissionApplicationService,
        IComplianceSchemeService complianceSchemeService)
    {
        _sessionManager = sessionManager;
        _userAccountService = userAccountService;
        _resubmissionApplicationService = resubmissionApplicationService;
        _submissionPeriods = globalVariables.Value.SubmissionPeriods;
        _complianceSchemeService = complianceSchemeService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.ResubmissionTaskList)]
    public async Task<IActionResult> ResubmissionTaskList()
    {
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
        var complianceSchemeId = session.RegistrationSession?.SelectedComplianceScheme?.Id;
        var complianceSchemeSummary = new ComplianceSchemeSummary();

        if (complianceSchemeId != null)
        {
            complianceSchemeSummary = await _complianceSchemeService.GetComplianceSchemeSummary(organisation.Id.Value, complianceSchemeId.Value);
        }

        var submissionPeriod = FindSubmissionPeriod(session.PomResubmissionSession.SubmissionPeriod);
        var submission = session.PomResubmissionSession.PomSubmission;

        var resubmissionApplicationDetails = await _resubmissionApplicationService.GetPackagingDataResubmissionApplicationDetails(
            organisation, new List<string> { session.PomResubmissionSession.SubmissionPeriod },
            complianceSchemeId);

        await UpdateSession(session, resubmissionApplicationDetails.First(), organisation, isComplianceScheme, complianceSchemeSummary, submissionPeriod);

        if (submission != null)
        {
            session.PomResubmissionSession.Journey = new List<string> { PagePaths.FileUploadSubLanding, $"/report-data{PagePaths.UploadNewFileToSubmit}?submissionId={submission.Id}", PagePaths.ResubmissionTaskList };

            if (string.IsNullOrEmpty(session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber))
            {
                var submittedByName = await GetUserNameFromId(submission.LastSubmittedFile.SubmittedBy!);
                var historyCount = await GetSubmissionHistory(submission, organisation.Id.Value, complianceSchemeId);
                await _resubmissionApplicationService.CreatePomResubmissionReferenceNumber(session, submittedByName, submission.Id, historyCount);
            }
        }

        await SaveSession(session, PagePaths.ResubmissionTaskList, PagePaths.ResubmissionFeeCalculations);

        return View(new ResubmissionTaskListViewModel
        {
            OrganisationName = organisation.Name!,
            OrganisationNumber = organisation.OrganisationNumber.ToReferenceNumberFormat(),
            IsComplianceScheme = isComplianceScheme,
            FileReachedSynapse = false,
            ApplicationStatus = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationStatus,
            FileUploadStatus = session.PomResubmissionSession.PackagingResubmissionApplicationSession.FileUploadStatus,
            PaymentViewStatus = session.PomResubmissionSession.PackagingResubmissionApplicationSession.PaymentViewStatus,
            AdditionalDetailsStatus = session.PomResubmissionSession.PackagingResubmissionApplicationSession.AdditionalDetailsStatus,
            IsResubmissionInProgress = session.PomResubmissionSession.PackagingResubmissionApplicationSession.IsResubmissionInProgress,
            ResubmissionApplicationSubmitted = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ResubmissionApplicationSubmitted
        });
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.ResubmissionFeeCalculations)]
    public async Task<IActionResult> ResubmissionFeeCalculations()
    {
        int memberCount = 0;
        var userData = User.GetUserData();
        var organisation = userData.Organisations[0];
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var complianceSchemeId = session.RegistrationSession?.SelectedComplianceScheme?.Id;
        session.PomResubmissionSession.Journey = new List<string> { $"/report-data/{PagePaths.ResubmissionTaskList}", PagePaths.ResubmissionFeeCalculations };
        SetBackLink(session, PagePaths.ResubmissionFeeCalculations);

        var applicationReferenceNumber = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber;
        var regulatorNation = session.PomResubmissionSession.RegulatorNation;
        var resubmissionDate = session.PomResubmissionSession.PomSubmission.LastSubmittedFile?.SubmittedDateTime;

        try
        {
            memberCount = await GetMemberCount(session.PomResubmissionSession.PackagingResubmissionApplicationSession.SubmissionId, isComplianceScheme, complianceSchemeId);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionRequired)
            {
                ViewData.ModelState.AddModelError("resubmission-fee-calculations", ex.Message);
                return View("ResubmissionFee", new ResubmissionFeeViewModel());
            }
            else
            {
                _logger.LogError("{message} for user '{userID}' in organisation '{organisationId}'", ex.Message, userData.Id.Value, organisation.Id.Value);
            }
        }

        var paymentResponse = await _resubmissionApplicationService.GetResubmissionFees(applicationReferenceNumber, regulatorNation, memberCount, isComplianceScheme, resubmissionDate);

        if (paymentResponse != null)
        {
            await _resubmissionApplicationService.CreatePackagingResubmissionFeeViewEvent(session.PomResubmissionSession.PackagingResubmissionApplicationSession.SubmissionId,
                                                                                          session.PomResubmissionSession.PomSubmission?.LastSubmittedFile?.FileId);

            UpdateResubmissionApplicationPaymentSession(session, paymentResponse);

            return View("ResubmissionFee", new ResubmissionFeeViewModel
            {
                IsComplianceScheme = isComplianceScheme,
                MemberCount = paymentResponse.MemberCount,
                PreviousPaymentsReceived = paymentResponse.PreviousPaymentsReceived,
                ResubmissionFee = paymentResponse.ResubmissionFee,
                TotalChargeableItems = paymentResponse.ResubmissionFee,
                TotalOutstanding = paymentResponse.TotalOutstanding
            });
        }

        return RedirectToAction(nameof(ResubmissionTaskList));
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.RedirectPackagingUploadDetails)]
    public async Task<IActionResult> RedirectToFileUpload()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        session.PomResubmissionSession.Journey = new List<string> { PagePaths.FileUploadSubLanding };
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return await RedirectToRightAction(session);
    }

    [HttpGet]
    [Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
    [Route(PagePaths.FileUploadResubmissionConfirmation)]
    public async Task<IActionResult> FileUploadResubmissionConfirmation()
    {
		var userData = User.GetUserData();
		var organisation = userData.Organisations[0];

		var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
		var complianceSchemeId = session.RegistrationSession.SelectedComplianceScheme?.Id;

		var resubmissionApplicationDetails = await _resubmissionApplicationService.GetPackagingDataResubmissionApplicationDetails(
			organisation, new List<string> { session.PomResubmissionSession.SubmissionPeriod },
			complianceSchemeId);

		var packagingResubmissionApplicationSession = resubmissionApplicationDetails[0].ToPackagingResubmissionApplicationSession(organisation);

		var lastSubmissionDate = packagingResubmissionApplicationSession.LastSubmittedFile?.SubmittedDateTime;

		if (lastSubmissionDate is null)
        {
			return RedirectToAction("Get", "FileUpload");
        }

        ViewBag.BackLinkToDisplay = Url.Content($"/report-data{PagePaths.FileUploadSubmissionDeclaration}");

        var model = new FileUploadResubmissionConfirmationViewModel
        {
            OrganisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole,
			SubmittedAt = lastSubmissionDate.Value.ToReadableDate(),
		};

        return View("FileUploadResubmissionConfirmation", model);
    }

    private async Task UpdateSession(FrontendSchemeRegistrationSession session, PackagingResubmissionApplicationDetails resubmissionApplicationDetails, EPR.Common.Authorization.Models.Organisation organisation, bool isComplianceScheme, ComplianceSchemeSummary complianceSchemeSummary, SubmissionPeriod submissionPeriod)
    {
        var packagingResubmissionApplicationSession = resubmissionApplicationDetails.ToPackagingResubmissionApplicationSession(organisation);

        session.PomResubmissionSession.PackagingResubmissionApplicationSession = packagingResubmissionApplicationSession;
        session.PomResubmissionSession.IsPomResubmissionJourney = true;
        session.PomResubmissionSession.Period = submissionPeriod;

        if (isComplianceScheme)
        {
            session.PomResubmissionSession.RegulatorNation = NationExtensions.GetNationNameFromId((int)complianceSchemeSummary.Nation);
        }
        else if (string.IsNullOrEmpty(session.PomResubmissionSession.RegulatorNation))
        {
            session.PomResubmissionSession.RegulatorNation = await _resubmissionApplicationService.GetRegulatorNation(organisation.Id);
        }

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    public async Task<int> GetMemberCount(Guid? submissionId, bool isComplianceScheme, Guid? complianceSchemeId)
    {
        if (!isComplianceScheme)
        {
            return 0;
        }

        var response = await _resubmissionApplicationService.GetPackagingResubmissionMemberDetails(new PackagingResubmissionMemberRequest()
        {
            SubmissionId = submissionId,
            ComplianceSchemeId = complianceSchemeId?.ToString()
        });

        if (response != null)
        {
            return response.MemberCount;
        }

        return 0;
    }

    public async Task<int?> GetSubmissionHistory(PomSubmission submission, Guid organisationId, Guid? complianceSchemeId)
    {
        var submissionPeriodIds = await _resubmissionApplicationService.GetSubmissionIdsAsync(organisationId, SubmissionType.Producer, complianceSchemeId, null);
        var submissionPeriod = submissionPeriodIds.Find(x => x.SubmissionId == submission.Id);
        var histories = submissionPeriod != null ? await _resubmissionApplicationService.GetSubmissionHistoryAsync(submission.Id, new DateTime(submissionPeriod.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)) : null;
        return histories?.Count;
    }

    private async Task<RedirectToActionResult> RedirectToRightAction(FrontendSchemeRegistrationSession session)
    {
        var submission = session.PomResubmissionSession.PomSubmission;

        var routeValueDictionary = new RouteValueDictionary { { "submissionId", submission?.Id } };

        if (SubmissionFileIdsDiffer(submission))
        {
            return RedirectToWarningController(routeValueDictionary);
        }

        return RedirectToAppropriateFileController(submission, routeValueDictionary);
    }

    private RedirectToActionResult RedirectToAppropriateFileController(PomSubmission submission, RouteValueDictionary routeValueDictionary)
    {
        return submission.LastSubmittedFile.FileId == submission.LastUploadedValidFile.FileId
            ? RedirectToAction(
                nameof(FileUploadController.Get),
                nameof(FileUploadController).RemoveControllerFromName(),
                routeValueDictionary)
            : RedirectToAction(
                nameof(FileUploadCheckFileAndSubmitController.Get),
                nameof(FileUploadCheckFileAndSubmitController).RemoveControllerFromName(),
                routeValueDictionary);
    }

    private RedirectToActionResult RedirectToWarningController(RouteValueDictionary routeValueDictionary)
    {
        return RedirectToAction(
            nameof(FileUploadWarningController.Get),
            nameof(FileUploadWarningController).RemoveControllerFromName(),
            routeValueDictionary);
    }

    private static bool SubmissionFileIdsDiffer(PomSubmission submission)
    {
        return submission.LastSubmittedFile.FileId != submission.LastUploadedValidFile.FileId &&
               submission.HasWarnings && submission.ValidationPass;
    }

    private async Task SaveSession(FrontendSchemeRegistrationSession session, string currentPagePath, string? nextPagePath)
    {
        ClearRestOfJourney(session, currentPagePath);

        session.PomResubmissionSession.Journey.AddIfNotExists(nextPagePath);

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }

    private static void ClearRestOfJourney(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        var index = session.PomResubmissionSession.Journey.IndexOf(currentPagePath);

        session.PomResubmissionSession.Journey = session.PomResubmissionSession.Journey.Take(index + 1).ToList();
    }

    private void SetBackLink(FrontendSchemeRegistrationSession session, string currentPagePath)
    {
        _logger.LogTrace("CurrentPagePath : {currentPagePath}", currentPagePath);

        ViewBag.BackLinkToDisplay = session.PomResubmissionSession.Journey.PreviousOrDefault(currentPagePath);
    }

    private SubmissionPeriod FindSubmissionPeriod(string dataPeriod)
    {
        return _submissionPeriods.Find(period => period.DataPeriod == dataPeriod);
    }

    private async Task<string> GetUserNameFromId(Guid userId)
    {
        var user = await _userAccountService.GetPersonByUserId(userId);
        return $"{user.FirstName} {user.LastName}";
    }

    private async Task UpdateResubmissionApplicationPaymentSession(FrontendSchemeRegistrationSession session, PackagingPaymentResponse packagingPaymentResponse)
    {
        session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding = packagingPaymentResponse.TotalOutstanding;
        session.PomResubmissionSession.FeeBreakdownDetails.MemberCount = packagingPaymentResponse.MemberCount;
        session.PomResubmissionSession.FeeBreakdownDetails.PreviousPaymentsReceived = packagingPaymentResponse.PreviousPaymentsReceived;
        session.PomResubmissionSession.FeeBreakdownDetails.ResubmissionFee = packagingPaymentResponse.ResubmissionFee;

        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);
    }
}