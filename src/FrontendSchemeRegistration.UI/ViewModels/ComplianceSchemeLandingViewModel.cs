namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeLandingViewModel
{
    public Guid CurrentComplianceSchemeId { get; set; }

    public string OrganisationName { get; set; }

    public List<ComplianceSchemeDto> ComplianceSchemes { get; set; }

    public ComplianceSchemeSummary CurrentTabSummary { get; set; }

    public NotificationViewModel Notification { get; set; } = new();

    public List<DatePeriod> SubmissionPeriods { get; set; } = new();

    public string CurrentYear => (DateTime.Now.Year).ToString();

    public bool IsApprovedUser { get; set; }

    public ResubmissionTaskListViewModel ResubmissionTaskListViewModel { get; set; }

    public List<RegistrationApplicationPerYearViewModel> RegistrationApplicationsPerYear { get; set; } = new();

}