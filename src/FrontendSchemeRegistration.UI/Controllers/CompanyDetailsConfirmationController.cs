namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Extensions;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
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

    public CompanyDetailsConfirmationController(
        ISubmissionService submissionService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IUserAccountService accountService)
    {
        _submissionService = submissionService;
        _sessionManager = sessionManager;
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");

        if (session is not null)
        {
            var organisationRole = session.UserData.Organisations?.FirstOrDefault()?.OrganisationRole;

            if (organisationRole is not null && Guid.TryParse(Request.Query["submissionId"], out var submissionId))
            {
                var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

                if (submission is not null && submission.IsSubmitted)
                {
                    var isFileUploadJourneyInvokedViaRegistration = session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration;

                    ViewBag.BackLinkToDisplay = isFileUploadJourneyInvokedViaRegistration ? $"/report-data/{PagePaths.RegistrationTaskList}" : Url.Content($"~{PagePaths.FileUploadCompanyDetailsSubLanding}");

                    ViewData["IsFileUploadJourneyInvokedViaRegistration"] = isFileUploadJourneyInvokedViaRegistration;

                    var submittedDateTime = submission.LastSubmittedFiles.SubmittedDateTime.Value;
                    return View(
                        "CompanyDetailsConfirmation",
                        new CompanyDetailsConfirmationModel
                        {
                            SubmittedDate = submittedDateTime.ToReadableDate(),
                            SubmissionTime = submittedDateTime.ToTimeHoursMinutes(),
                            SubmittedBy = await GetUsersName(submission.LastSubmittedFiles.SubmittedBy.Value),
                            OrganisationRole = organisationRole
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
