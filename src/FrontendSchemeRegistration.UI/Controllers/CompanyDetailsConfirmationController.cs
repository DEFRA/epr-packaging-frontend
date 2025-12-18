namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Extensions;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sessions;
using ViewModels;

[Authorize(Policy = PolicyConstants.EprFileUploadPolicy)]
[Route(PagePaths.CompanyDetailsConfirmation)]
public class CompanyDetailsConfirmationController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IUserAccountService _accountService;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public CompanyDetailsConfirmationController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IUserAccountService accountService,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _accountService = accountService;
        _registrationApplicationService = registrationApplicationService;
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        var registrationYear = _registrationApplicationService.ValidateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");

        if (session is not null)
        {
            var organisation = session.UserData.Organisations?.FirstOrDefault();
            var organisationRole = organisation?.OrganisationRole;

            if (organisationRole is not null && Guid.TryParse(Request.Query["submissionId"], out var submissionId))
            {
                var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

                if (submission is not null && submission.IsSubmitted)
                {
                    var isInvokedViaRegistration = session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration;
                    this.SetBackLink(
                        isInvokedViaRegistration, 
                        session.RegistrationSession.IsResubmission,
                        registrationYear,
                        submission.RegistrationJourney);

                    ViewData["IsFileUploadJourneyInvokedViaRegistration"] = isInvokedViaRegistration;

                    var submittedDateTime = submission.LastSubmittedFiles.SubmittedDateTime.Value;
                    var routeValue = QueryStringExtensions.BuildRouteValues(
                        isResubmission : session.RegistrationSession.IsResubmission,
                        registrationYear: registrationYear);
                    
                    bool showRegistrationCaption = organisationRole == OrganisationRoles.ComplianceScheme 
                                                   && submission.RegistrationJourney is not null 
                                                   && registrationYear is not null;
                    return View(
                        "CompanyDetailsConfirmation",
                        new CompanyDetailsConfirmationModel
                        {
                            ShowRegistrationCaption = showRegistrationCaption,
                            SubmittedDate = submittedDateTime.ToReadableDate(),
                            SubmissionTime = submittedDateTime.ToTimeHoursMinutes(),
                            SubmittedBy = await GetUsersName(submission.LastSubmittedFiles.SubmittedBy.Value),
                            OrganisationRole = organisationRole,
                            IsResubmission = session.RegistrationSession.IsResubmission,
                            ReturnToRegistrationLink = Url.Action("RegistrationTaskList", "RegistrationApplication", routeValue),
                            RegistrationYear = registrationYear,
                            RegistrationJourney = submission.RegistrationJourney,
                            OrganisationName = organisation.Name,
                        });
                }
            }
        }

        return RedirectToAction("Get", "Landing");
    }

    private async Task<string> GetUsersName(Guid userId)
    {
        var person = await _accountService.GetPersonByUserId(userId);
        return person.GetUserName();
    }
}