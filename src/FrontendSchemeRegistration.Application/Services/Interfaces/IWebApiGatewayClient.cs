using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
using FrontendSchemeRegistration.Application.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IWebApiGatewayClient
{
    Task<Guid> UploadFileAsync(
        byte[] byteArray,
        string fileName,
        string submissionPeriod,
        Guid? submissionId,
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null, 
        bool? isResubmission = null);

    Task<Guid> UploadSubsidiaryFileAsync(
        byte[] byteArray,
        string fileName,
        Guid? submissionId,
        SubmissionType submissionType,
        Guid? complianceSchemeId = null);

    Task<T?> GetSubmissionAsync<T>(Guid id)
        where T : AbstractSubmission;

    Task<List<T>> GetSubmissionsAsync<T>(string queryString)
        where T : AbstractSubmission;

    Task<List<ProducerValidationError>> GetProducerValidationErrorsAsync(Guid submissionId);

    Task<List<RegistrationValidationError>> GetRegistrationValidationErrorsAsync(Guid submissionId);

    Task SubmitAsync(Guid submissionId, SubmissionPayload payload);

    Task CreateRegistrationApplicationEvent(Guid submissionId, RegistrationApplicationPayload applicationPayload);

    Task<T> GetDecisionsAsync<T>(string queryString) where T : AbstractDecision;

    Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, string queryString);

    Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, string queryString);

    Task<List<PrnModel>> GetPrnsForLoggedOnUserAsync();

    Task<PrnModel> GetPrnByExternalIdAsync(Guid id);

    Task SetPrnApprovalStatusToAcceptedAsync(Guid id);

    Task SetPrnApprovalStatusToAcceptedAsync(Guid[] ids);

    Task SetPrnApprovalStatusToRejectedAsync(Guid id);

    Task<UploadFileErrorResponse> GetSubsidiaryFileUploadStatusAsync(Guid userId, Guid organisationId);

	Task<PaginatedResponse<PrnModel>> GetSearchPrnsAsync(PaginatedRequest request);

	Task<SubsidiaryUploadStatusDto> GetSubsidiaryUploadStatus(Guid userId, Guid organisationId);

    Task<PrnObligationModel> GetRecyclingObligationsCalculation(List<Guid> externalIds, int year);

    Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request);

    Task<byte[]?> FileDownloadAsync(string queryString);

    Task<PackagingResubmissionApplicationDetails?> GetPackagingDataResubmissionApplicationDetails(GetPackagingResubmissionApplicationDetailsRequest request);

    Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request);

	Task CreatePackagingResubmissionReferenceNumberEvent(Guid submissionId, PackagingResubmissionReferenceNumberCreatedEvent @event);

    Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId);

    Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId,string paymentMethod);

    Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment);
}