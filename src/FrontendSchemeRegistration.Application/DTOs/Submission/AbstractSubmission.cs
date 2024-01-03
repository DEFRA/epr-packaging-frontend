using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public abstract class AbstractSubmission
{
    public Guid Id { get; set; }

    public abstract SubmissionType Type { get; }

    public string SubmissionPeriod { get; set; }

    public bool ValidationPass { get; set; }

    public bool HasWarnings { get; set; }

    public bool HasValidFile { get; set; }

    public List<string> Errors { get; set; } = new ();

    public bool IsSubmitted { get; set; }
}