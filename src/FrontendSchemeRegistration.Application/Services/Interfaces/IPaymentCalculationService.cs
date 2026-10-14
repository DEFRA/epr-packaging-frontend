using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IPaymentCalculationService
{
    Task<PaymentCalculationResponse?> GetProducerRegistrationFees(PaymentCalculationRequest request);

    Task<PaymentCalculationResponse?> GetProducerRegistrationFeesBySubmissionId(Guid submissionId);

    Task<string> GetRegulatorNation(Guid? organisationId);

    Task<string> InitiatePayment(PaymentInitiationRequest request);

    Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationRequest request);

    Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFeesBySubmissionId(Guid submissionId);
	Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate);

    Task<RegistrationFeeCalculationDetails[]?> GetRegistrationFeeCalculationDetails(Guid submissionId);

    /// <summary>
    /// Whether the payment service holds registration data for the submission. Only a 404 means "not known";
    /// any other failure throws, so an outage is never mistaken for a submission the payment service has not seen.
    /// </summary>
    Task<bool> IsSubmissionKnownToPaymentServiceAsync(Guid submissionId);

    Task<SubmissionPeriodDetails[]> GetSubmissionPeriods();
}