using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class SubmissionPeriodDetail
{
    public string DataPeriod { get; set; }

    public string DatePeriodYear { get; set; }

    public string DatePeriodStartMonth { get; set; }

    public string DatePeriodEndMonth { get; set; }

    public string DatePeriodShortStartMonth { get; set; }
    public string DatePeriodShortEndMonth { get; set; }

    public DateTime Deadline { get; set; }

    public SubmissionPeriodStatus Status { get; set; }

    public bool IsResubmissionRequired { get; set; }

    public string? Decision { get; set; }

    public string? Comments { get; set; } = string.Empty;

    public bool IsSubmitted { get; set; }

    public bool? IsResubmissionComplete { get; set; }

    // SUB-332: IsResubmissionComplete stays gated on the fee and Synapse sync, so between declaring and the
    // sync completing it reads false while IsResubmissionInProgress is also false. This carries the
    // declaration itself so the tile keeps an action during that window.
    public bool ResubmissionApplicationSubmitted { get; set; }

    // SUB-345: a regulator decision closes the resubmission cycle it ruled on, and the cycle's own fields
    // stop being reported at that point. This carries the fact that the cycle happened, so the tile can offer
    // a look back at a completed resubmission rather than the start of another one.
    public bool HasCompletedResubmission { get; set; }

    public InProgressSubmissionPeriodStatus? InProgressSubmissionPeriodStatus { get; set; }

    public string ApplicationStatus { get; set; }

    public string FileUploadStatus { get; set; }
}