using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IPaymentCalculationService
{
    Task<PaymentCalculationResponse> GetProducerRegistrationFees(ProducerDetailsDto producerDetails,
        string applicationReferenceNumber, bool isLateFeeApplicable, Guid? organisationId, DateTime registrationSubmissionDate);

    Task<string> GetRegulatorNation(Guid? organisationId);

    Task<string> InitiatePayment(PaymentInitiationRequest request);

    Task<ComplianceSchemePaymentCalculationResponse> GetComplianceSchemeRegistrationFees(ComplianceSchemeDetailsDto complianceSchemeDetails, string applicationReferenceNumber, Guid? organisationId);

    Task<ComplianceSchemeDetailsDto> GetComplianceSchemeDetails(string organisationId);

    string CreateApplicationReferenceNumber(bool isComplianceScheme, int csRowNumber, string organisationNumber, SubmissionPeriod period);
}
