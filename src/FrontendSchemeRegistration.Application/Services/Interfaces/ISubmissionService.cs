using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ISubmissionService
{
    Task<T?> GetSubmissionAsync<T>(Guid submissionId) where T : AbstractSubmission;

    Task<List<T>> GetSubmissionsAsync<T>(List<string> periods, int? limit, Guid? complianceSchemeId) where T : AbstractSubmission;

    Task SubmitAsync(Guid submissionId, Guid fileId);

    Task SubmitAsync(Guid submissionId, Guid fileId, string submittedBy, string? appReferenceNumber = null, bool? isResubmitted = null);

    Task CreateRegistrationApplicationEvent(Guid submissionId, Guid? complianceSchemeId, string? comments, string? paymentMethod, string applicationReferenceNumber, bool isResubmission, SubmissionType submissionType);
    
    Task<T> GetDecisionAsync<T>(int? limit, Guid submissionId, SubmissionType type) where T : AbstractDecision;

    Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, SubmissionType type, Guid? complianceSchemeId, int? year);

    Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime);

    Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request);

    Task<PackagingResubmissionApplicationDetails?> GetPackagingDataResubmissionApplicationDetails(GetPackagingResubmissionApplicationDetailsRequest request);

    Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request);

	Task CreatePackagingResubmissionReferenceNumberEvent(Guid submissionId, PackagingResubmissionReferenceNumberCreatedEvent @event);

    Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId);

    Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod);

    Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId,string submittedBy, DateTime submissionDate, string comment);
}