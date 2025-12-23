using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Text.Json.Serialization;
using Enums;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationDetails
{
    public ApplicationStatusType ApplicationStatus { get; set; }
    
    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }
    
	public bool? IsResubmission { get; set; }
	
    public string? RegistrationFeePaymentMethod { get; set; }

    public bool HasAnyApprovedOrQueriedRegulatorDecision { get; set; }

    public bool IsLatestSubmittedEventAfterFileUpload { get; set; }

    public DateTime? LatestSubmittedEventCreatedDatetime { get; set; }

    public DateTime? FirstApplicationSubmittedEventCreatedDatetime { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }
    
    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public string? RegistrationReferenceNumber { get; set; }

    public RegistrationFeeCalculationDetails[]? RegistrationFeeCalculationDetails { get; set; }
    
    public ComplianceSchemeDto? SelectedComplianceScheme { get; set; }
    
    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();
    
    [JsonConverter( typeof(JsonStringEnumConverter))]
    public RegistrationJourney? RegistrationJourney { get; set; }
}