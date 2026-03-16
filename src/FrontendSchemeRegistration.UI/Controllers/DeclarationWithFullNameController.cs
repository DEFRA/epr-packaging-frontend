using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Enums;
using Application.Extensions;
using Constants;

[Route(PagePaths.DeclarationWithFullName)]
public class DeclarationWithFullNameController(
    ISubmissionService submissionService,
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    ILogger<DeclarationWithFullNameController> logger,
    IRegistrationPeriodProvider registrationPeriodProvider) : Controller
{
    private const string ViewName = "DeclarationWithFullName";
    private const string ConfirmationViewName = "CompanyDetailsConfirmation";
    private const string SubmissionErrorViewName = "OrganisationDetailsSubmissionFailed";

    [HttpGet]
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

                session.EnsureApplicationReferenceIsPresent();

                await submissionService.SubmitAsync(submissionId, new Guid(model.OrganisationDetailsFileId),
                    model.FullName,
                    session.RegistrationSession.ApplicationReferenceNumber,
                    session.RegistrationSession.IsResubmission,
                    regJourney);

                return (model.RegistrationYear.HasValue
                    ? RedirectToAction("Get", ConfirmationViewName,
                        new
                        {
                            submissionId, registrationyear = model.RegistrationYear.ToString(),
                            registrationjourney = regJourney
                        })
                    : RedirectToAction("Get", ConfirmationViewName, new { submissionId }));
            }
            catch (Exception)
            {
                return RedirectToAction("Get", SubmissionErrorViewName, new { submissionId });
            }
        }
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