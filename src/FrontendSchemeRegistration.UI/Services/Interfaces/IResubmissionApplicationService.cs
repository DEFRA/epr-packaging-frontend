using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Sessions;
using System.Security.Claims;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IResubmissionApplicationService
    {
        Task<string> GetRegulatorNation(Guid? organisationId);

        Task<string> CreatePomResubmissionReferenceNumberForProducer(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId, int? historyCount);

        Task<string> CreatePomResubmissionReferenceNumberForCSO(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId, int? historyCount);

        Task<string> CreatePomResubmissionReferenceNumber(FrontendSchemeRegistrationSession session, string submittedByName, Guid submissionId, int? historyCount);

        Task<List<PackagingResubmissionApplicationDetails>> GetPackagingDataResubmissionApplicationDetails(Organisation organisation, List<string> submissionPeriods, Guid? complianceSchemeId);

        Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession);

        Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request);

        Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate);

        Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId);

        Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod);

        Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment);

        Task<List<PackagingResubmissionApplicationSession>> GetPackagingResubmissionApplicationSession(Organisation organisation, List<string> submissionPeriods, Guid? complianceSchemeId);

        Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, SubmissionType type, Guid? complianceSchemeId, int? year);

        Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime);
    }
}