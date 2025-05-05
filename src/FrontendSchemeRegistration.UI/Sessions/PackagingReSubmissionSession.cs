using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class PackagingReSubmissionSession
{
    public List<string> Journey { get; set; } = new();

    public Guid? FileId { get; set; }

    public List<PomSubmission> PomSubmissions { get; set; } = new();

    public PomSubmission PomSubmission { get; set; }

    public string? SubmissionPeriod { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public SubmissionPeriod Period { get; set; }

    public string RegulatorNation { get; set; } = string.Empty;

    public bool IsPomResubmissionJourney { get; set; }

    public PackagingResubmissionApplicationSession PackagingResubmissionApplicationSession { get; set; } = new();

    public FeeBreakdownDetails FeeBreakdownDetails { get; set; } = new();
}