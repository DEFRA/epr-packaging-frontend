using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class SubmissionPayload
{
    public Guid FileId { get; set; }

    public string? SubmittedBy { get; set; }
}