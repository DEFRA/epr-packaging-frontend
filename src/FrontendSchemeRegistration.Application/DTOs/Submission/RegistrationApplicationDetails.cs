using System.Diagnostics.CodeAnalysis;
namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationDetails
{
    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public string? RegistrationReferenceNumber { get; set; }

    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();

    public string? RegistrationFeePaymentMethod { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }

    public ApplicationStatusType ApplicationStatus { get; set; }

    public ProducerDetailsDto? ProducerDetails { get; set; }

    public List<ComplianceSchemeDetailsMemberDto>? CsoMemberDetails { get; set; }
}