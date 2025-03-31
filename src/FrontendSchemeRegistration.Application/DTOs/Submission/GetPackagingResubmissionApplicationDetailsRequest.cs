using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;

[ExcludeFromCodeCoverage]
public class GetPackagingResubmissionApplicationDetailsRequest
{
    public Guid OrganisationId { get; set; }

    public int OrganisationNumber { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public List<string> SubmissionPeriods { get; set; } = new List<string>();
}