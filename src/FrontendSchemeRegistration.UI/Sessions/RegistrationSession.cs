namespace FrontendSchemeRegistration.UI.Sessions;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;
using Enums;

[ExcludeFromCodeCoverage]
public class RegistrationSession
{
    public List<string> Journey { get; set; } = new();

    public string OrganisationNumber { get; set; }

    public Guid? SubmissionId { get; set; }

    public Guid? FileId { get; set; }

    public ComplianceSchemeDto? SelectedComplianceScheme { get; set; }

    public ProducerComplianceSchemeDto? CurrentComplianceScheme { get; set; }

    public bool IsUpdateJourney { get; set; }

    public string? SubmissionPeriod { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public Dictionary<string, Guid> LatestRegistrationSet { get; set; } = new();

    public bool? UsingAComplianceScheme { get; set; }

    public ChangeComplianceSchemeOptions? ChangeComplianceSchemeOptions { get; set; }

    public string? NotificationMessage { get; set; }
}
