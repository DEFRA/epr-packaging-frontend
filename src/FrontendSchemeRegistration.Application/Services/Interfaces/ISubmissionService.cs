namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.Submission;

public interface ISubmissionService
{
    Task<T?> GetSubmissionAsync<T>(Guid submissionId)
        where T : AbstractSubmission;

    Task<List<T>> GetSubmissionsAsync<T>(
        List<string> periods,
        int? limit,
        Guid? complianceSchemeId,
        bool? isFirstComplianceScheme)
        where T : AbstractSubmission;

    Task SubmitAsync(Guid submissionId, Guid fileId);

    Task SubmitAsync(Guid submissionId, Guid fileId, string submittedBy);
}