namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs;
using DTOs.Submission;
using Enums;

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

    Task<T?> GetSubmissionAsync<T>(Guid id)
        where T : AbstractSubmission;

    Task<List<T>> GetSubmissionsAsync<T>(string queryString)
        where T : AbstractSubmission;

    Task<List<ProducerValidationError>> GetProducerValidationErrorsAsync(Guid submissionId);

    Task SubmitAsync(Guid submissionId, SubmissionPayload payload);
}