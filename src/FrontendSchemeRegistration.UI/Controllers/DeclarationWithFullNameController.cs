namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Extensions;
using Microsoft.AspNetCore.Mvc;
using UI.Attributes.ActionFilters;
using ViewModels;

[Route(PagePaths.DeclarationWithFullName)]
public class DeclarationWithFullNameController : Controller
{
    private const string ViewName = "DeclarationWithFullName";
    private const string ConfirmationViewName = "CompanyDetailsConfirmation";
    private const string SubmissionErrorViewName = "OrganisationDetailsSubmissionFailed";

    private readonly ISubmissionService _submissionService;

    public DeclarationWithFullNameController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var userData = User.GetUserData();

        if (!userData.CanSubmit())
        {
            var routeValues = new RouteValueDictionary { { "submissionId", submissionId.ToString() } };
            return RedirectToAction("Get", "ReviewCompanyDetails", routeValues);
        }

        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadSubLanding");
        }

        var reviewOrganisationDataPath = PagePaths.ReviewOrganisationData.StartsWith("/")
            ? PagePaths.ReviewOrganisationData
            : Path.Combine("/", PagePaths.ReviewOrganisationData);

        ViewBag.BackLinkToDisplay = Url.Content($"~{reviewOrganisationDataPath}?submissionId={submissionId}");

        return View(ViewName, new DeclarationWithFullNameViewModel
        {
            OrganisationName = User.GetUserData().Organisations.First()?.Name,
            OrganisationDetailsFileId = submission.LastUploadedValidFiles.CompanyDetailsFileId.ToString(),
            SubmissionId = submissionId
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

        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        ViewBag.BackLinkToDisplay = Url.Content($"~{PagePaths.FileUploadSubLanding}");

        if (!ModelState.IsValid)
        {
            return View(ViewName, model);
        }

        try
        {
            await _submissionService.SubmitAsync(submissionId, new Guid(model.OrganisationDetailsFileId), model.FullName);
            return RedirectToAction("Get", ConfirmationViewName, new { submissionId });
        }
        catch (Exception)
        {
            return RedirectToAction("Get", SubmissionErrorViewName, new { submissionId });
        }
    }
}