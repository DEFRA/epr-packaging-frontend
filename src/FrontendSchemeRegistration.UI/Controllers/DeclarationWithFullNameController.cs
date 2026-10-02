using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Enums;
using Application.Extensions;
using Constants;

[Route(PagePaths.DeclarationWithFullName)]
public class DeclarationWithFullNameController(
    ISubmissionService submissionService,
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    ISessionManager<RegistrationApplicationSession> registrationApplicationSessionManager,
    ILogger<DeclarationWithFullNameController> logger,
    IRegistrationPeriodProvider registrationPeriodProvider,
    IFeatureManager featureManager,
    IPaymentCalculationService paymentCalculationService) : Controller
{
    private const string ViewName = "DeclarationWithFullName";
    private const string ConfirmationViewName = "CompanyDetailsConfirmation";
    private const string ProcessingViewName = "DeclarationProcessing";
    private const string SubmissionErrorViewName = "OrganisationDetailsSubmissionFailed";

    [HttpGet]
    [RegistrationApplicationSessionLoggingScopeActionFilter]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get([FromQuery]Guid submissionId, [FromQuery] RegistrationJourney? registrationJourney = null)
    {
        var userData = User.GetUserData();
        var registrationYear = registrationPeriodProvider.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (!userData.CanSubmit())
        {
            var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear);
            return RedirectToAction("Get", "ReviewCompanyDetails", routeValues);
        }

        var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        if (!submission.HasValidFile)
        {
            logger.LogError("User {UserId} loaded a page with no valid submission files for submission ID {SubmissionId}", userData.Id, submissionId);
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var organisation = userData.Organisations[0];
        bool isCso = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        var regJourney = submission.RegistrationJourney ?? registrationJourney;

        SetBackLink(submissionId, registrationYear, regJourney);

        return View(ViewName, new DeclarationWithFullNameViewModel
        {
            OrganisationName = organisation.Name,
            OrganisationDetailsFileId = submission.LastUploadedValidFiles.CompanyDetailsFileId.ToString(),
            SubmissionId = submissionId,
            IsResubmission = session.RegistrationSession.IsResubmission,
            RegistrationYear = registrationYear,
            RegistrationJourney = regJourney,
            ShowRegistrationCaption = isCso && regJourney is not null && registrationYear is not null,
            IsCso = isCso
        });
    }

    [RegistrationApplicationSessionLoggingScopeActionFilter]
    [HttpPost]
    public async Task<IActionResult> Post([FromQuery]Guid submissionId, DeclarationWithFullNameViewModel model)
    {
        using (logger.BeginScope("Http"))
        using (logger.BeginScope("Submit declaration with full name"))
        using (logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = submissionId,
                   ["OrganisationName"] = model.OrganisationName,
                   ["RegistrationYear"] = model.RegistrationYear,
                   ["RegistrationJourney"] = model.RegistrationJourney,
                   ["IsResubmission"] = model.IsResubmission,
                   ["IsCso"] = model.IsCso,
                   ["OrganisationDetailsFileId"] = model.OrganisationDetailsFileId
               }))
        {
            if (!ModelState.IsValid)
            {
                SetBackLink(submissionId, model.RegistrationYear, model.RegistrationJourney);
                return View(ViewName, model);
            }

            var userData = User.GetUserData();

            if (!userData.CanSubmit())
            {
                var routeValues = QueryStringExtensions.BuildRouteValues(submissionId: submissionId,
                    registrationYear: model.RegistrationYear, registrationJourney: model.RegistrationJourney);
                return RedirectToAction("Get", "ReviewCompanyDetails", routeValues);
            }

            logger.LogInformation("Submitting declaration with full name");
            var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

            if (submission is null)
            {
                return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
            }

            if (!submission.HasValidFile)
            {
                logger.LogError(
                    "Blocked User {UserId} attempted post of full name for a submission {SubmissionId} with no valid files",
                    userData.Id, submissionId);
                return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
            }

            ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

            try
            {
                var regJourney = submission.RegistrationJourney ?? model.RegistrationJourney;

                var session = await sessionManager.GetSessionAsync(HttpContext.Session);
                if (session?.RegistrationSession is null)
                {
                    logger.LogError("RegistrationSession not found for submission ID {SubmissionId}", submissionId);
                    throw new InvalidOperationException($"RegistrationSession not found for submission ID {submissionId}");
                }

                var registrationYear = ParseRegistrationYear(submission.SubmissionPeriod) ?? model.RegistrationYear;
                var applicationDetails = await GetApplicationDetailsForSubmissionAsync(submission, regJourney, userData, session);
                var isResubmission = applicationDetails is not null
                    ? applicationDetails.IsResubmission ?? false
                    : session.RegistrationSession.IsResubmission;
                var applicationReferenceNumber = ResolveApplicationReferenceNumber(submission, session, applicationDetails);

                if (string.IsNullOrWhiteSpace(applicationReferenceNumber))
                {
                    logger.LogError("Application reference number is missing for submission ID {SubmissionId}", submissionId);
                    throw new ArgumentException($"Application reference number is missing for submission ID {submissionId}");
                }

                logger.LogInformation("Calling submission service to submit registration for submission ID {SubmissionId}", submissionId);

                var organisationDetailsFileId = new Guid(model.OrganisationDetailsFileId);

                var registrationApplicationSession = await registrationApplicationSessionManager.GetSessionAsync(HttpContext.Session);

                var regulatorNation = ResolveRegulatorNation(model, session, userData, registrationApplicationSession);

                if (string.IsNullOrWhiteSpace(regulatorNation))
                {
                    logger.LogError("RegulatorNation could not be resolved for submission ID {SubmissionId}", submissionId);
                    throw new ArgumentException($"RegulatorNation could not be resolved for submission ID {submissionId}");
                }

                var notifyPaymentService = await ShouldNotifyPaymentServiceAsync(isResubmission, submission, submissionId);

                var submissionPeriodId = ResolveSubmissionPeriodId(registrationYear, regJourney, userData, registrationApplicationSession);

                await submissionService.SubmitAsync(submissionId, organisationDetailsFileId,
                    model.FullName,
                    applicationReferenceNumber,
                    isResubmission,
                    regJourney,
                    new RegistrationSubmitContext
                    {
                        SubmissionPeriodId = submissionPeriodId,
                        RegulatorNation = regulatorNation,
                        NotifyPaymentService = notifyPaymentService
                    });

                var postSubmitController = (await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeParametersViaPaymentService) && notifyPaymentService)
                    ? ProcessingViewName
                    : ConfirmationViewName;

                return (model.RegistrationYear.HasValue
                    ? RedirectToAction("Get", postSubmitController,
                        new
                        {
                            submissionId, registrationyear = model.RegistrationYear.ToString(),
                            registrationjourney = regJourney
                        })
                    : RedirectToAction("Get", postSubmitController, new { submissionId }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while submitting declaration with full name for submission ID {SubmissionId}", submissionId);
                return RedirectToAction("Get", SubmissionErrorViewName, new { submissionId });
            }
        }
    }

    private static string? ResolveRegulatorNation(
        DeclarationWithFullNameViewModel model,
        FrontendSchemeRegistrationSession session,
        UserData userData,
        RegistrationApplicationSession? registrationApplicationSession)
    {
        var regulatorNation = registrationApplicationSession?.RegulatorNation;
        if (!string.IsNullOrWhiteSpace(regulatorNation))
        {
            return regulatorNation;
        }

        var nationId = model.IsCso
            ? session.RegistrationSession.SelectedComplianceScheme?.NationId
            : userData.Organisations[0].NationId;

        return nationId.HasValue
            ? NationExtensions.GetNationNameFromId(nationId.Value)
            : null;
    }

    // The registration sessions are shared across registration years, so their values reflect whichever year's
    // task list / file upload journey was last entered. Resolve per-submission values from the submission instead.
    private async Task<RegistrationApplicationDetails?> GetApplicationDetailsForSubmissionAsync(
        RegistrationSubmission submission,
        RegistrationJourney? regJourney,
        UserData userData,
        FrontendSchemeRegistrationSession session)
    {
        var organisation = userData.Organisations[0];
        if (string.IsNullOrWhiteSpace(submission.SubmissionPeriod)
            || organisation.Id is null
            || !int.TryParse(organisation.OrganisationNumber, out var organisationNumber))
        {
            return null;
        }

        var details = await submissionService.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = organisationNumber,
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = session.RegistrationSession.SelectedComplianceScheme?.Id,
            SubmissionPeriod = submission.SubmissionPeriod,
            RegistrationJourney = regJourney?.ToString()
        });

        return details?.SubmissionId == submission.Id ? details : null;
    }

    // A session app ref cached for a different registration year must never be sent; leaving it unresolved
    // routes the user to the error page so they re-enter this year's journey.
    private static string? ResolveApplicationReferenceNumber(
        RegistrationSubmission submission,
        FrontendSchemeRegistrationSession session,
        RegistrationApplicationDetails? applicationDetails)
    {
        if (!string.IsNullOrWhiteSpace(applicationDetails?.ApplicationReferenceNumber))
        {
            return applicationDetails.ApplicationReferenceNumber;
        }

        var sessionIsForSubmissionPeriod = string.IsNullOrWhiteSpace(submission.SubmissionPeriod)
            || string.Equals(session.RegistrationSession.SubmissionPeriod, submission.SubmissionPeriod, StringComparison.Ordinal);

        return sessionIsForSubmissionPeriod ? session.RegistrationSession.ApplicationReferenceNumber : null;
    }

    private int? ResolveSubmissionPeriodId(
        int? registrationYear,
        RegistrationJourney? regJourney,
        UserData userData,
        RegistrationApplicationSession? registrationApplicationSession)
    {
        if (registrationYear is null)
        {
            return registrationApplicationSession?.SubmissionPeriodId;
        }

        var isCso = userData.Organisations[0].OrganisationRole == OrganisationRoles.ComplianceScheme;
        var isSmallProducer = regJourney?.ToString().Contains("Small", StringComparison.OrdinalIgnoreCase) ?? false;

        return registrationPeriodProvider.GetRegistrationWindow(isCso, isSmallProducer, registrationYear.Value)?.Id;
    }

    private static int? ParseRegistrationYear(string? submissionPeriod)
    {
        var lastToken = submissionPeriod?.Trim().Split(' ').LastOrDefault();
        return int.TryParse(lastToken, out var year) ? year : null;
    }

    private async Task<bool> ShouldNotifyPaymentServiceAsync(
        bool isResubmission,
        RegistrationSubmission submission,
        Guid submissionId)
    {
        if (!isResubmission || !submission.IsSubmitted)
        {
            return true;
        }

        var feeParams = await paymentCalculationService.GetRegistrationFeeCalculationDetails(submissionId);
        var notify = feeParams is { Length: > 0 };
        if (!notify)
        {
            logger.SuppressingPaymentServiceNotification(submissionId);
        }

        return notify;
    }

    private void SetBackLink(Guid submissionId, int? registrationYear, RegistrationJourney? regJourney)
    {
        var reviewOrganisationDataPath = PagePaths.ReviewOrganisationData.StartsWith('/')
            ? PagePaths.ReviewOrganisationData
            : Path.Combine("/", PagePaths.ReviewOrganisationData);
        var routeValue = QueryStringExtensions.BuildRouteValues(submissionId: submissionId, registrationYear: registrationYear, registrationJourney: regJourney);
        ViewBag.BackLinkToDisplay = QueryHelpers.AddQueryString(Url.Content($"~{reviewOrganisationDataPath}"), routeValue.ToDictionary(k => k.Key, k => k.Value.ToString() ?? string.Empty));
    }

}

internal static partial class DeclarationWithFullNameControllerLog
{
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Suppressing payment-service notification for legacy resubmission {SubmissionId} with no payment-service snapshot")]
    public static partial void SuppressingPaymentServiceNotification(this ILogger logger, Guid submissionId);
}