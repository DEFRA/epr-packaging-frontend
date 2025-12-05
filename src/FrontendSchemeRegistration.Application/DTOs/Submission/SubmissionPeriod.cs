using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

/// <summary>
/// This is used to represent both a POM submission period AND a registration submission period.
/// This class is instantiated when binding appSettings data in startup, AND instantiated manually.
/// When instantiated from appSettings, it represents a POM submission period. When instantiated elsewhere
/// it can also represent a registration submission period.
/// TODO - use two DTOs to represent the two different types of submission periods
/// </summary>
[ExcludeFromCodeCoverage]
public class SubmissionPeriod
{
    public string DataPeriod { get; init; }

    public string StartMonth { get; init; }

    public string EndMonth { get; init; }

    public string Year { get; init; }

    public DateTime Deadline { get; init; }

    public DateTime ActiveFrom { get; init; }
}
