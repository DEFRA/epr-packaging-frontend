using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Controllers;

[Route(PagePaths.DeclarationWithFullName)]
public class DeclarationWithFullNameController(
    ISubmissionService submissionService,
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    ILogger<DeclarationWithFullNameController> logger) : Controller
{
    private const string ViewName = "DeclarationWithFullName";
    private const string ConfirmationViewName = "CompanyDetailsConfirmation";
    private const string SubmissionErrorViewName = "OrganisationDetailsSubmissionFailed";

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        var session = await sessionManager.GetSessionAsync(HttpContext.Session);

        if (!userData.CanSubmit())
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
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

        var reviewOrganisationDataPath = PagePaths.ReviewOrganisationData.StartsWith('/')
            ? PagePaths.ReviewOrganisationData
            : Path.Combine("/", PagePaths.ReviewOrganisationData);

        ViewBag.BackLinkToDisplay = Url.Content($"~{reviewOrganisationDataPath}?submissionId={submissionId}");

        return View(ViewName, new DeclarationWithFullNameViewModel
        {
            OrganisationName = User.GetUserData().Organisations[0].Name,
            OrganisationDetailsFileId = submission.LastUploadedValidFiles.CompanyDetailsFileId.ToString(),
            SubmissionId = submissionId,
            IsResubmission = session.RegistrationSession.IsResubmission
        });
    }

    [HttpPost]
    public async Task<IActionResult> Post(DeclarationWithFullNameViewModel model)
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        if (!userData.CanSubmit())
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction("Get", "ReviewCompanyDetails", routeValues);
        }

        var submission = await submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        if (!submission.HasValidFile)
        {
            logger.LogError("Blocked User {UserId} attempted post of full name for a submission {SubmissionId} with no valid files", userData.Id, submissionId);
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        if (!ModelState.IsValid)
        {
            return View(ViewName, model);
        }

        try
        {
            var session = await sessionManager.GetSessionAsync(HttpContext.Session);

            await submissionService.SubmitAsync(submissionId, new Guid(model.OrganisationDetailsFileId), model.FullName, session.RegistrationSession.ApplicationReferenceNumber, session.RegistrationSession.IsResubmission);

            return RedirectToAction("Get", ConfirmationViewName, new { submissionId });
        }
        catch (Exception)
        {
            return RedirectToAction("Get", SubmissionErrorViewName, new { submissionId });
        }
    }
}