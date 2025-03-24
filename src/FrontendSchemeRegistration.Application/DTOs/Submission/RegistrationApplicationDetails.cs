using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationDetails
{
    public ApplicationStatusType ApplicationStatus { get; set; }
    
    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }
    
	public bool? IsResubmission { get; set; }
	
    public string? RegistrationFeePaymentMethod { get; set; }

    public bool IsLateFeeApplicable { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }
    
    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public string? RegistrationReferenceNumber { get; set; }

    public RegistrationFeeCalculationDetails[]? RegistrationFeeCalculationDetails { get; set; }
    
    public ComplianceSchemeDto? SelectedComplianceScheme { get; set; }
    
    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();
}