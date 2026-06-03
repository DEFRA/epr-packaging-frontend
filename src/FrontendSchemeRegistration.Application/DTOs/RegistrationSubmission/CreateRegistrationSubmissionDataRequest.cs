namespace FrontendSchemeRegistration.Application.DTOs.RegistrationSubmission;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CreateRegistrationSubmissionDataRequest
{
    public Guid SubmissionId { get; set; }

    public Guid FileId { get; set; }

    public string RegistrationBlobName { get; set; } = string.Empty;

    public Guid? ComplianceSchemeId { get; set; }

    public string SubmissionPeriod { get; set; } = string.Empty;

    public DateTime SubmissionDate { get; set; }
}
