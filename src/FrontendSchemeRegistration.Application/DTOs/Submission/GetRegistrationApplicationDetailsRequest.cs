using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class GetRegistrationApplicationDetailsRequest
{ 
    public Guid OrganisationId { get; set; }
    public int OrganisationNumber { get; set; }
 
    public Guid? ComplianceSchemeId { get; set; }

    public string SubmissionPeriod { get; set; } = null!;
}