using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

[ExcludeFromCodeCoverage]
public class ComplianceSchemePaymentCalculationRequest
{
    public string Regulator { get; set; }
    public string ApplicationReferenceNumber { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string ProduerSize { get; set; }
    public List<ComplianceSchemePaymentCalculationRequestMember> ComplianceSchemeMembers { get; set; }
    public bool IncludeRegistrationFee { get; set; } = true;
}