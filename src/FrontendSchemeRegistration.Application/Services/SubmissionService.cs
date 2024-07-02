using System.Web;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;

namespace FrontendSchemeRegistration.Application.Services;

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
        Guid? complianceSchemeId)
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

    public async Task<T> GetDecisionAsync<T>(
        int? limit,
        Guid submissionId,
        SubmissionType type)
        where T : AbstractDecision
    {
        var queryString = $"";

        if (limit is > 0)
        {
            queryString += $"limit={limit}";
            queryString += $"&";
        }

        queryString += $"submissionId={submissionId}&type={type}";

        return await _webApiGatewayClient.GetDecisionsAsync<T>(queryString);
    }

    public async Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, SubmissionType type, Guid? complianceSchemeId, int? year)
    {
        var queryString = $"type={type}";

        if (complianceSchemeId is not null && complianceSchemeId != Guid.Empty)
        {
            queryString += $"&complianceSchemeId={complianceSchemeId}";
        }

        if (year is not null)
        {
            queryString += $"&year={year}";
        }

        return await _webApiGatewayClient.GetSubmissionIdsAsync(organisationId, queryString);
    }

    public async Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime)
    {
        return await _webApiGatewayClient.GetSubmissionHistoryAsync(submissionId, $"lastSyncTime={lastSyncTime:s}");
    }
}