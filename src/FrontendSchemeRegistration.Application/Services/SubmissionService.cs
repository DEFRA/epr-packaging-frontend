namespace FrontendSchemeRegistration.Application.Services;

using System.Web;
using DTOs.Submission;
using Interfaces;

public class SubmissionService : ISubmissionService
{
    private readonly IWebApiGatewayClient _webApiGatewayClient;

    public SubmissionService(IWebApiGatewayClient webApiGatewayClient)
    {
        _webApiGatewayClient = webApiGatewayClient;
    }

    public async Task<T> GetSubmissionAsync<T>(Guid submissionId)
        where T : AbstractSubmission
    {
        return await _webApiGatewayClient.GetSubmissionAsync<T>(submissionId);
    }

    public async Task<List<T>> GetSubmissionsAsync<T>(
        List<string> periods,
        int? limit,
        Guid? complianceSchemeId,
        bool? isFirstComplianceScheme)
        where T : AbstractSubmission
    {
        var type = Activator.CreateInstance<T>().Type;
        var queryString = $"type={type}";

        if (periods.Any())
        {
            queryString += $"&periods={HttpUtility.UrlEncode(string.Join(",", periods))}";
        }

        if (limit is > 0)
        {
            queryString += $"&limit={limit}";
        }

        if (complianceSchemeId is not null)
        {
            queryString += $"&complianceSchemeId={complianceSchemeId}";
        }

        if (isFirstComplianceScheme is not null)
        {
            queryString += $"&isFirstComplianceScheme={isFirstComplianceScheme}";
        }

        return await _webApiGatewayClient.GetSubmissionsAsync<T>(queryString);
    }

    public async Task SubmitAsync(Guid submissionId, Guid fileId)
    {
        await SubmitAsync(submissionId, fileId, null);
    }

    public async Task SubmitAsync(Guid submissionId, Guid fileId, string? submittedBy)
    {
        var payload = new SubmissionPayload
        {
            FileId = fileId,
            SubmittedBy = submittedBy
        };

        await _webApiGatewayClient.SubmitAsync(submissionId, payload);
    }
}