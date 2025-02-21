using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IPaymentCalculationService
{
    Task<PaymentCalculationResponse?> GetProducerRegistrationFees(PaymentCalculationRequest request);

    Task<string> InitiatePayment(PaymentInitiationRequest request);

    Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationRequest request);
}
