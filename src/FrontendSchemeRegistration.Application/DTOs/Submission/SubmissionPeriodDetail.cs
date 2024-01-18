using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class SubmissionPeriodDetail
{
    public string DataPeriod { get; set; }

    public DateTime Deadline { get; set; }

    public SubmissionPeriodStatus Status { get; set; }

    public bool IsResubmissionRequired { get; set; }

    public string? Decision { get; set; }

    public string? Comments { get; set; } = string.Empty;
}