using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

/// <summary>
/// SUB-345: a resubmission cycle the regulator has already ruled on.
/// </summary>
/// <remarks>
/// Every other field on <see cref="PackagingResubmissionApplicationDetails"/> describes the cycle that is open
/// now, so all of them stop reporting a cycle at the decision that closed it. Without this, a completed
/// resubmission is indistinguishable from one that was never started, which is why an accepted resubmission
/// used to leave the sub-landing tile offering to begin the journey again.
/// </remarks>
[ExcludeFromCodeCoverage]
public class CompletedResubmissionDetails
{
    public string? ApplicationReferenceNumber { get; set; }

    public string? ResubmissionReferenceNumber { get; set; }

    public DateTime? DeclarationDate { get; set; }

    public string? DeclarationComment { get; set; }

    public string? DeclaredByName { get; set; }

    public bool? IsResubmissionFeeViewed { get; set; }

    public string? ResubmissionFeePaymentMethod { get; set; }

    public string? Decision { get; set; }

    public string? RegulatorComments { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string? FileName { get; set; }

    public LastSubmittedFileDetails? SubmittedFile { get; set; }
}
