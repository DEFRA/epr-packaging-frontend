using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using Enums;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationPayload
{
    public string? ApplicationReferenceNumber { get; set; }
    
    public Guid? ComplianceSchemeId { get; set; }
    
    public string? PaymentMethod { get; set; }

    public string PaymentStatus { get; set; }

    public string PaidAmount { get; set; }

    public SubmissionType SubmissionType { get; set; }
    public string? Comments { get; set; }
    
    public bool IsResubmission { get; set; }
    
    public string? RegistrationJourney { get; set; }
}