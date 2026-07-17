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

    // Has this been submitted for fee calculation (submit 1)
    public bool IsSubmitted { get; set; }
    
	public bool? IsResubmission { get; set; }
	
    public string? RegistrationFeePaymentMethod { get; set; }

    public bool HasAnyApprovedOrQueriedRegulatorDecision { get; set; }

    // is the most recent submission (submit 1) after the latest successful (and validated) file upload? If not,
    // then the file has been successfully uploaded, but not submitted (submit 1), and is therefore ready to submit (submit 1)
    public bool IsLatestSubmittedEventAfterFileUpload { get; set; }

    // this is the last time that the file was submitted for fee calculation (submit 1)
    public DateTime? LatestSubmittedEventCreatedDatetime { get; set; }

    // this is the first time the submission was submitted to the Regulator for review (submit 2)
    public DateTime? FirstApplicationSubmittedEventCreatedDatetime { get; set; }

    // this is the last time the submission was submitted to the Regulator for review (submit 2)
    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }
    
    // This is generated when the application is submitted for fee calculation (submit 1), eg PEPR1690421049026P1L
    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    // This is generated when the registration is granted by the regulator, eg R26EC169042104392L
    public string? RegistrationReferenceNumber { get; set; }

    // These are the details that are needed to calculate the registration fee
    public RegistrationFeeCalculationDetails[]? RegistrationFeeCalculationDetails { get; set; }
    
    public ComplianceSchemeDto? SelectedComplianceScheme { get; set; }
    
    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();
    
    [JsonConverter( typeof(JsonStringEnumConverter))]
    public RegistrationJourney? RegistrationJourney { get; set; }
}