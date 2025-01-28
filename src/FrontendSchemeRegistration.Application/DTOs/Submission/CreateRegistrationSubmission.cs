using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class CreateRegistrationSubmission
{
    public Guid Id { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string? AppReferenceNumber { get; set; }
}

public enum DataSourceType
{
    File = 1
}
