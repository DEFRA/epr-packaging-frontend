namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsSubLandingViewModel : ViewModelWithOrganisationRole
{
    public string? ComplianceSchemeName { get; set; }

    public List<SubmissionPeriodDetailGroup> SubmissionPeriodDetailGroups { get; set; }
}