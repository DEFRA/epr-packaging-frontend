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
    IRegistrationApplicationService registrationApplicationService,
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

                var submitDetails = await registrationApplicationService.ResolveRegistrationSubmitDetailsAsync(
                    HttpContext.Session, userData.Organisations[0], submission, regJourney, model.RegistrationYear);

                if (string.IsNullOrWhiteSpace(submitDetails.ApplicationReferenceNumber))
                {
                    logger.LogError("Application reference number is missing for submission ID {SubmissionId}", submissionId);
                    throw new ArgumentException($"Application reference number is missing for submission ID {submissionId}");
                }

                logger.LogInformation("Calling submission service to submit registration for submission ID {SubmissionId}", submissionId);

                var organisationDetailsFileId = new Guid(model.OrganisationDetailsFileId);

                if (string.IsNullOrWhiteSpace(submitDetails.RegulatorNation))
                {
                    logger.LogError("RegulatorNation could not be resolved for submission ID {SubmissionId}", submissionId);
                    throw new ArgumentException($"RegulatorNation could not be resolved for submission ID {submissionId}");
                }

                var notifyPaymentService = await ShouldNotifyPaymentServiceAsync(submitDetails, submission, submissionId);

                await submissionService.SubmitAsync(submissionId, organisationDetailsFileId,
                    model.FullName,
                    submitDetails.ApplicationReferenceNumber,
                    submitDetails.IsResubmission,
                    regJourney,
                    new RegistrationSubmitContext
                    {
                        SubmissionPeriodId = submitDetails.SubmissionPeriodId,
                        RegulatorNation = submitDetails.RegulatorNation,
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

    // A declaration whose fees depend on earlier history - a resubmission, or any declaration after an application was
    // already submitted (e.g. after a regulator query, where IsResubmission stays false) - is only sent to the payment
    // service if it already holds this submission. Otherwise the payment service would start from a partial history
    // and treat this cycle as the first, so the submission stays on the legacy fee path instead.
    private async Task<bool> ShouldNotifyPaymentServiceAsync(
        RegistrationSubmitDetails submitDetails,
        RegistrationSubmission submission,
        Guid submissionId)
    {
        if (!submission.IsSubmitted || (!submitDetails.IsResubmission && !submitDetails.HasPreviousApplicationSubmission))
        {
            return true;
        }

        // Throws on anything other than a 404, so the declaration fails and can be retried rather than
        // being moved to the legacy path because the payment service was briefly unavailable.
        var notify = await paymentCalculationService.IsSubmissionKnownToPaymentServiceAsync(submissionId);
        if (!notify)
        {
            logger.SuppressingPaymentServiceNotification(submissionId, submitDetails.IsResubmission, submitDetails.HasPreviousApplicationSubmission);
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
        Message = "Suppressing payment-service notification for submission {SubmissionId} not known to the payment service (IsResubmission {IsResubmission}, HasPreviousApplicationSubmission {HasPreviousApplicationSubmission})")]
    public static partial void SuppressingPaymentServiceNotification(this ILogger logger, Guid submissionId, bool isResubmission, bool hasPreviousApplicationSubmission);
}