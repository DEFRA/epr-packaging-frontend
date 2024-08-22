using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;

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
        Guid? complianceSchemeId = null);

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

    Task<T> GetDecisionsAsync<T>(string queryString)
        where T : AbstractDecision;

    Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, string queryString);

    Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, string queryString);

    Task<List<SubsidiaryExportDto>> GetSubsidiariesAsync(int subsidiaryParentId);
}