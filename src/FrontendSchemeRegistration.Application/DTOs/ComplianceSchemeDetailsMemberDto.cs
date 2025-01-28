namespace FrontendSchemeRegistration.Application.DTOs;

public class ComplianceSchemeDetailsMemberDto
{
    public string MemberId { get; set; } = string.Empty;    // OrganisationNumber
    public string MemberType { get; set; } = string.Empty;
    public bool IsOnlineMarketplace { get; set; }
    public bool IsLateFeeApplicable { get; set; } = false;
    public int NumberOfSubsidiaries { get; set; }
    public int NumberOfSubsidiariesBeingOnlineMarketPlace { get; set; }
}