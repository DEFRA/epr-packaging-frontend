using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Constants;

namespace FrontendSchemeRegistration.UI.ViewModels;

public class FileUploadSubLandingViewModel
{
    public string? ComplianceSchemeName { get; set; }

    public string? OrganisationRole { get; set; }

    public bool IsComplianceScheme => OrganisationRole == OrganisationRoles.ComplianceScheme;

    public string ServiceRole { get; set; } = "Basic User";

    public List<SubmissionPeriodDetailGroup> SubmissionPeriodDetailGroups { get; set; }
}