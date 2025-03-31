using System.Diagnostics.CodeAnalysis;
namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class PackagingResubmissionApplicationDetails
{
    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

	public bool? IsResubmitted { get; set; }

	public bool? IsResubmissionFeeViewed { get; set; }

	public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public LastSubmittedFileDetails? LastSubmittedFile { get; set; }

    public string? ResubmissionFeePaymentMethod { get; set; }

    public DateTime? ResubmissionApplicationSubmittedDate { get; set; }

    public string? ResubmissionApplicationSubmittedComment { get; set; }

    public bool ResubmissionApplicationSubmitted => ResubmissionApplicationSubmittedDate is not null;

    public ApplicationStatusType ApplicationStatus { get; set; }
    
    public string? ResubmissionReferenceNumber { get; set; }

    public SynapseResponse SynapseResponse { get; set; } = new();
}