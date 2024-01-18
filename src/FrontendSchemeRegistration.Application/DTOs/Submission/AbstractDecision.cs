using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class AbstractDecision
{
    public Guid SubmissionId { get; set; }

    public string Comments { get; set; } = string.Empty;

    public string Decision { get; set; } = string.Empty;

    public bool IsResubmissionRequired { get; set; }
}