namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.DTOs.Submission;

public class FileUploadCompanyDetailsSubLandingViewModel : ViewModelWithOrganisationRole
{
    public string? ComplianceSchemeName { get; set; }

    public List<SubmissionPeriodDetail> SubmissionPeriodDetails { get; set; }
}