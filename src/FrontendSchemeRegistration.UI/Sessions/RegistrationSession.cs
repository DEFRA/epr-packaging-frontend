using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.UI.Enums;

namespace FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class RegistrationSession
{
    public List<string> Journey { get; set; } = new();

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

    public bool IsFileUploadJourneyInvokedViaRegistration { get; set; }

    public bool IsResubmission { get; set; }
    
    public string ApplicationReferenceNumber { get; set; }
}