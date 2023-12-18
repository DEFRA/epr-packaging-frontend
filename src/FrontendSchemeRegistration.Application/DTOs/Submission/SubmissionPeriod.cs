using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class SubmissionPeriod
{
    public string DataPeriod { get; init; }

    public string StartMonth { get; init; }

    public string EndMonth { get; init; }

    public DateTime Deadline { get; init; }

    public DateTime ActiveFrom { get; init; }
}