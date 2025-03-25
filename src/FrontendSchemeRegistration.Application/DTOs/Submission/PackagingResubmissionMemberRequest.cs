using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class PackagingResubmissionMemberRequest
{
    public Guid? SubmissionId { get; set; }

    public string ComplianceSchemeId { get; set; }
}
