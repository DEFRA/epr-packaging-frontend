using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Enums;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.ReviewOrganisationData)]
public class ReviewCompanyDetailsController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IUserAccountService _accountService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly ILogger<ReviewCompanyDetailsController> _logger;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public ReviewCompanyDetailsController(
        ISubmissionService submissionService,
        IUserAccountService accountService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        ILogger<ReviewCompanyDetailsController> logger,
        IRegistrationApplicationService registrationApplicationService)

    {
        _submissionService = submissionService;
        _accountService = accountService;
        _sessionManager = sessionManager;
        _logger = logger;
        _registrationApplicationService = registrationApplicationService;
    }

    [HttpGet]
    [SubmissionIdActionFilter("/error")]
    public async Task<IActionResult> Get([FromQuery] RegistrationJourney? registrationJourney)
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();
        var registrationYear = _registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (!submission.HasValidFile)
        {
            _logger.LogError("User {UserId} loaded a page with no valid submission files for submission ID {SubmissionId}", userData.Id, submissionId);
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        var isFileUploadJourneyInvokedViaRegistration = session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration;
        var isResubmission = session.RegistrationSession.IsResubmission;

        this.SetBackLink(isFileUploadJourneyInvokedViaRegistration, isResubmission, registrationYear, submission.RegistrationJourney);
        ViewData["IsFileUploadJourneyInvokedViaRegistration"] = isFileUploadJourneyInvokedViaRegistration;

        return View(
            "ReviewCompanyDetails",
            new ReviewCompanyDetailsViewModel
            {
                SubmissionId = submissionId,
                OrganisationDetailsFileName = submission.LastUploadedValidFiles.CompanyDetailsFileName,
                OrganisationDetailsUploadedBy = await GetUsersName(submission.LastUploadedValidFiles.CompanyDetailsUploadedBy),
                OrganisationDetailsFileUploadDate =
                    submission.LastUploadedValidFiles.CompanyDetailsUploadDatetime.ToReadableDate(),
                OrganisationDetailsFileId = submission.LastUploadedValidFiles.CompanyDetailsFileId.ToString(),
                BrandFileName = submission.LastUploadedValidFiles?.BrandsFileName,
                BrandUploadedBy = submission.LastUploadedValidFiles?.BrandsUploadedBy != null
                    ? await GetUsersName(submission.LastUploadedValidFiles.BrandsUploadedBy.Value)
                    : string.Empty,
                BrandFileUploadDate = submission.LastUploadedValidFiles?.BrandsUploadDatetime?.ToReadableDate(),
                PartnerFileName = submission.LastUploadedValidFiles?.PartnershipsFileName,
                PartnerUploadedBy = submission.LastUploadedValidFiles?.PartnershipsUploadedBy != null
                    ? await GetUsersName(submission.LastUploadedValidFiles.PartnershipsUploadedBy.Value)
                    : string.Empty,
                PartnerFileUploadDate = submission.LastUploadedValidFiles?.PartnershipsUploadDatetime?.ToReadableDate(),
                RegistrationSubmissionDeadline = session.RegistrationSession.SubmissionDeadline.ToReadableLongMonthDeadlineDate(),
                BrandsRequired = submission.LastUploadedValidFiles is not null && !string.IsNullOrWhiteSpace(submission.LastUploadedValidFiles.BrandsFileName),
                PartnersRequired = submission.LastUploadedValidFiles is not null && !string.IsNullOrWhiteSpace(submission.LastUploadedValidFiles.PartnershipsFileName),
                OrganisationRole = userData.Organisations.FirstOrDefault()!.OrganisationRole,
                IsApprovedUser = userData.CanSubmit(),
                HasPreviousSubmission = submission.LastSubmittedFiles is not null,
                HasPreviousBrandsSubmission = submission.LastSubmittedFiles is not null && !string.IsNullOrWhiteSpace(submission.LastSubmittedFiles.BrandsFileName),
                HasPreviousPartnersSubmission = submission.LastSubmittedFiles is not null && !string.IsNullOrWhiteSpace(submission.LastSubmittedFiles.PartnersFileName),
                SubmittedCompanyDetailsFileName = submission.LastSubmittedFiles?.CompanyDetailsFileName,
                SubmittedCompanyDetailsDateTime = submission.LastSubmittedFiles?.SubmittedDateTime?.ToReadableDate(),
                SubmittedBrandsFileName = submission.LastSubmittedFiles?.BrandsFileName,
                SubmittedBrandsDateTime = submission.LastSubmittedFiles?.SubmittedDateTime?.ToReadableDate(),
                SubmittedPartnersFileName = submission.LastSubmittedFiles?.PartnersFileName,
                SubmittedPartnersDateTime = submission.LastSubmittedFiles?.SubmittedDateTime?.ToReadableDate(),
                SubmittedDateTime = submission.LastSubmittedFiles?.SubmittedDateTime?.ToReadableDate(),
                SubmittedBy = submission.LastSubmittedFiles?.SubmittedBy != null
                    ? await GetUsersName(submission.LastSubmittedFiles.SubmittedBy.Value)
                    : string.Empty,
                SubmissionStatus = submission.GetSubmissionStatus(),
                IsResubmission = isResubmission,
                RegistrationYear = registrationYear,
                RegistrationJourney = submission.RegistrationJourney
            });

        return RedirectToAction("HandleThrownExceptions", "Error");
    }

    [HttpPost]
    public async Task<IActionResult> Post(ReviewCompanyDetailsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("ReviewCompanyDetails", model);
        }

        var submissionId = Guid.Parse(Request.Query["submissionId"]);

        var userData = User.GetUserData();

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");

        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var isFileUploadJourneyInvokedViaRegistration = session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration;
        ViewData["IsFileUploadJourneyInvokedViaRegistration"] = isFileUploadJourneyInvokedViaRegistration;

        if (!userData.CanSubmit())
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction("Get", routeValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (!submission.HasValidFile)
        {
            _logger.LogError("Blocked User {UserId} attempted post of a submission {SubmissionId} with no valid files", userData.Id, submissionId);
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (model.SubmitOrganisationDetailsResponse.HasValue && !model.SubmitOrganisationDetailsResponse.Value)
        {
            if (isFileUploadJourneyInvokedViaRegistration)
                if (model.RegistrationYear.HasValue)
                    return Redirect(QueryHelpers.AddQueryString(PagePaths.RegistrationTaskList, "registrationyear", model.RegistrationYear.ToString()));
                else
                    return Redirect(PagePaths.RegistrationTaskList);

            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (!model.IsApprovedUser)
        {
            return new UnauthorizedResult();
        }

        try
        {
            var routeValue = QueryStringExtensions.BuildRouteValues(submissionId: model.SubmissionId, registrationYear: model.RegistrationYear, registrationJourney: model.RegistrationJourney);

            return RedirectToAction("Get", "DeclarationWithFullName", routeValue);
        }
        catch (Exception)
        {
            return RedirectToAction("HandleThrownSubmissionException", "Error");
        }
    }

    private async Task<string> GetUsersName(Guid userId)
    {
        var person = await _accountService.GetAllPersonByUserId(userId);
        return $"{person.FirstName} {person.LastName}";
    }
}