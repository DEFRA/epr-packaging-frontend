namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class SubmissionPeriodDetail
{
    public string DataPeriod { get; set; }

    public DateTime Deadline { get; set; }

    public SubmissionPeriodStatus Status { get; set; }
}