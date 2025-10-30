namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

public class ComplianceSchemePaymentCalculationRequestMember
{
    public string MemberId { get; set; }
    public string MemberType { get; set; }
    public bool IsOnlineMarketplace { get; set; }
    public bool IsLateFeeApplicable { get; set; }
    public int NumberOfSubsidiaries { get; set; }
    public int NoOfSubsidiariesOnlineMarketplace { get; set; }
    public int NumberofLateSubsidiaries { get; set; }
}