using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ISubmissionService
{
    Task<T?> GetSubmissionAsync<T>(Guid submissionId) where T : AbstractSubmission;

    Task<List<T>> GetSubmissionsAsync<T>(List<string> periods, int? limit, Guid? complianceSchemeId) where T : AbstractSubmission;

    Task SubmitAsync(Guid submissionId, Guid fileId);

    Task SubmitAsync(Guid submissionId, Guid fileId, string submittedBy, string? appReferenceNumber = null);

    Task SubmitRegistrationApplicationAsync(Guid submissionId, Guid? complianceSchemeId, string? comments, string? paymentMethod, string applicationReferenceNumber, SubmissionType submissionType);

    Task<T> GetDecisionAsync<T>(int? limit, Guid submissionId, SubmissionType type) where T : AbstractDecision;

    Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, SubmissionType type, Guid? complianceSchemeId, int? year);

    Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime);

    Task<bool> HasSubmissionsAsync(Guid organiationExternalId, SubmissionType type, Guid? complianceSchemeId);

    Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request);
}