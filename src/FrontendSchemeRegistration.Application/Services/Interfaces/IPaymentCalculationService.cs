using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IPaymentCalculationService
{
    Task<PaymentCalculationResponse?> GetProducerRegistrationFees(PaymentCalculationRequest request);

	Task<string> GetRegulatorNation(Guid? organisationId);
    
    Task<string> InitiatePayment(PaymentInitiationRequest request);

    Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationRequest request);
	
	Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate);
}