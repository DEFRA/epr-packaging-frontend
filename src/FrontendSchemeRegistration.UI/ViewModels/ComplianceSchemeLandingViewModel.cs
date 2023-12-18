namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Submission;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeLandingViewModel
{
    public Guid CurrentComplianceSchemeId { get; set; }

    public string OrganisationName { get; set; }

    public List<ComplianceSchemeDto> ComplianceSchemes { get; set; }

    public ComplianceSchemeSummary CurrentTabSummary { get; set; }

    public NotificationViewModel Notification { get; set; } = new();

    public List<DatePeriod> SubmissionPeriods { get; set; } = new();

    public bool IsApprovedUser { get; set; }
}