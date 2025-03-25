using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;
using System.Security.Claims;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
	public interface IResubmissionApplicationService
	{
		Task<string> GetRegulatorNation(Guid? organisationId);

		Task<string> CreatePomResubmissionReferenceNumberForProducer(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId);

        Task<string> CreatePomResubmissionReferenceNumberForCSO(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId);

		Task<string> CreatePomResubmissionReferenceNumber(FrontendSchemeRegistrationSession session, string submittedByName, Guid submissionId);

        Task<PackagingResubmissionApplicationDetails> GetPackagingDataResubmissionApplicationDetails(Organisation organisation, string submissionPeriod, Guid? complianceSchemeId);

		Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession);

		Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request);

		Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate);

		Task SubmitAsync(Guid submissionId, Guid fileId, string submittedBy, string? appReferenceNumber = null, bool? isResubmitted = null);

        Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId);

        Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod);

		Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment);
    }
}