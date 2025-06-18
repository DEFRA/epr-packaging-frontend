namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Sessions;
using Extensions;
using global::FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Resources.Views.FileReUploadConfirmation;
using Sessions;
using UI.Attributes.ActionFilters;
using ViewModels;

[Route(PagePaths.FileReUploadCompanyDetailsConfirmation)]
public class FileReUploadCompanyDetailsConfirmationController : Controller
{
    private readonly ISubmissionService _submissionService;
    private readonly IUserAccountService _accountService;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
    private readonly IRegistrationApplicationService _registrationApplicationService;

    public FileReUploadCompanyDetailsConfirmationController(
        ISubmissionService submissionService,
        IUserAccountService accountService,
        ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
        IRegistrationApplicationService registrationApplicationService)
    {
        _submissionService = submissionService;
        _accountService = accountService;
        _sessionManager = sessionManager;
        _registrationApplicationService = registrationApplicationService;

    }

    [HttpGet]
    [SubmissionIdActionFilter(PagePaths.FileUploadCompanyDetailsSubLanding)]
    public async Task<IActionResult> Get()
    {
        var registrationYear = await _registrationApplicationService.validateRegistrationYear(HttpContext.Request.Query["registrationyear"], true);
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session);
        if (session is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var isFileUploadJourneyInvokedViaRegistration = session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration;

        this.SetBackLink(isFileUploadJourneyInvokedViaRegistration, session.RegistrationSession.IsResubmission, registrationYear);
        ViewData["IsFileUploadJourneyInvokedViaRegistration"] = isFileUploadJourneyInvokedViaRegistration;

        var organisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole;
        var submissionId = Guid.Parse(Request.Query["submissionId"]);
        var submission = await _submissionService.GetSubmissionAsync<RegistrationSubmission>(submissionId);

        if (organisationRole is null || submission is null)
        {
            return RedirectToAction("Get", "FileUploadCompanyDetailsSubLanding");
        }

        var model = await GetViewModel(submission, session);

        session.RegistrationSession.Journey.AddIfNotExists(PagePaths.FileUploadCompanyDetailsSubLanding);
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return View(nameof(FileReUploadCompanyDetailsConfirmation), model);
    }

    private async Task<FileReUploadCompanyDetailsConfirmationViewModel> GetViewModel(
        RegistrationSubmission submission,
        FrontendSchemeRegistrationSession session)
    {
        var model = new FileReUploadCompanyDetailsConfirmationViewModel
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted,
            IsApprovedUser = session.UserData.ServiceRole.Parse<ServiceRole>()
                .In(ServiceRole.Delegated, ServiceRole.Approved),
            Status = submission.GetSubmissionStatus(),
            SubmissionDeadline = session.RegistrationSession.SubmissionDeadline.ToReadableLongMonthDeadlineDate(),
            OrganisationRole = session.UserData.Organisations.FirstOrDefault()?.OrganisationRole,
            HasValidfile = submission.HasValidFile
        };

        if (model.Status.Equals(SubmissionPeriodStatus.FileUploaded) ||
            model.Status.Equals(SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload))
        {
            var companyDetailsUploadedBy = await GetUsersName(submission.LastUploadedValidFiles.CompanyDetailsUploadedBy);
            var brandsFileUploadedBy = await GetUsersName(submission.LastUploadedValidFiles.BrandsUploadedBy);
            var partnersFileUploadedBy = await GetUsersName(submission.LastUploadedValidFiles.PartnershipsUploadedBy);

            model.CompanyDetailsFileName = submission.LastUploadedValidFiles.CompanyDetailsFileName;
            model.CompanyDetailsFileUploadDate = submission.LastUploadedValidFiles.CompanyDetailsUploadDatetime.ToReadableDate();
            model.CompanyDetailsFileUploadedBy = companyDetailsUploadedBy.UserName;
            model.IsCompanyDetailsFileUploadedByDeleted = companyDetailsUploadedBy.IsDeleted;
            model.BrandsFileName = submission.LastUploadedValidFiles.BrandsFileName;
            model.BrandsFileUploadDate = submission.LastUploadedValidFiles.BrandsUploadDatetime.GetValueOrDefault().ToReadableDate();
            model.BrandsFileUploadedBy = brandsFileUploadedBy.UserName;
            model.IsBrandsFileUploadedByDeleted = brandsFileUploadedBy.IsDeleted;
            model.PartnersFileName = submission.LastUploadedValidFiles.PartnershipsFileName;
            model.PartnersFileUploadDate =
                submission.LastUploadedValidFiles.PartnershipsUploadDatetime.GetValueOrDefault().ToReadableDate();
            model.PartnersFileUploadedBy = partnersFileUploadedBy.UserName;
            model.IsPartnersFileUploadedByDeleted = partnersFileUploadedBy.IsDeleted;
        }
        else if (model.Status.Equals(SubmissionPeriodStatus.SubmittedToRegulator))
        {
            var submittedByUser = await GetUsersName(submission.LastSubmittedFiles.SubmittedBy);
            model.CompanyDetailsFileName = submission.LastSubmittedFiles.CompanyDetailsFileName;
            model.CompanyDetailsFileUploadDate = submission.LastSubmittedFiles.SubmittedDateTime
                .GetValueOrDefault().ToReadableDate();
            model.CompanyDetailsFileUploadedBy = submittedByUser.UserName;
            model.IsCompanyDetailsFileUploadedByDeleted = submittedByUser.IsDeleted;
            model.BrandsFileName = submission.LastSubmittedFiles.BrandsFileName;
            model.BrandsFileUploadDate = submission.LastSubmittedFiles.SubmittedDateTime.GetValueOrDefault()
                .ToReadableDate();
            model.BrandsFileUploadedBy = submittedByUser.UserName;
            model.IsBrandsFileUploadedByDeleted = submittedByUser.IsDeleted;
            model.PartnersFileName = submission.LastSubmittedFiles.PartnersFileName;
            model.PartnersFileUploadDate = submission.LastSubmittedFiles.SubmittedDateTime.GetValueOrDefault()
                .ToReadableDate();
            model.PartnersFileUploadedBy = submittedByUser.UserName;
            model.IsPartnersFileUploadedByDeleted = submittedByUser.IsDeleted;
        }

        return model;
    }

    private async Task<(string UserName, bool IsDeleted)> GetUsersName(Guid? userId)
    {
        if (userId == null)
        {
            return (null, false);
        }

        var person = await _accountService.GetAllPersonByUserId(userId.Value);
        return (person != null ? $"{person.FirstName} {person.LastName}" : null, person is not null && person.IsDeleted);
    }

}