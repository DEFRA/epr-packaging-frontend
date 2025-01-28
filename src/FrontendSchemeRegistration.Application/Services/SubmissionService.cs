using System.Web;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;

namespace FrontendSchemeRegistration.Application.Services;

public class SubmissionService(IWebApiGatewayClient webApiGatewayClient) : ISubmissionService
{
    public async Task<T> GetSubmissionAsync<T>(Guid submissionId)
        where T : AbstractSubmission
    {
        return await webApiGatewayClient.GetSubmissionAsync<T>(submissionId);
    }

    public async Task<List<T>> GetSubmissionsAsync<T>(List<string> periods, int? limit, Guid? complianceSchemeId)
        where T : AbstractSubmission
    {
        var type = Activator.CreateInstance<T>().Type;
        var queryString = $"type={type}";

        if (periods.Count > 0)
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

        return await webApiGatewayClient.GetSubmissionsAsync<T>(queryString);
    }

    public async Task SubmitAsync(Guid submissionId, Guid fileId)
    {
        await SubmitAsync(submissionId, fileId, null);
    }

    public async Task SubmitAsync(Guid submissionId, Guid fileId, string? submittedBy, string? appReferenceNumber = null)
    {
        var payload = new SubmissionPayload
        {
            FileId = fileId,
            SubmittedBy = submittedBy,
            AppReferenceNumber = appReferenceNumber
        };

        await webApiGatewayClient.SubmitAsync(submissionId, payload);
    }

    public async Task SubmitRegistrationApplicationAsync(Guid submissionId, Guid? complianceSchemeId, string? comments, string? paymentMethod, string applicationReferenceNumber, SubmissionType submissionType)
    {
        var applicationPayload = new RegistrationApplicationPayload
        {
            ApplicationReferenceNumber = applicationReferenceNumber,
            ComplianceSchemeId = complianceSchemeId,
            PaymentMethod = paymentMethod ?? string.Empty,
            PaymentStatus = "Not-Applicable",
            PaidAmount = "0",
            Comments = comments,
            SubmissionType = submissionType
        };
        await webApiGatewayClient.SubmitRegistrationApplication(submissionId, applicationPayload);
    }

    public async Task<T> GetDecisionAsync<T>(int? limit, Guid submissionId, SubmissionType type)
        where T : AbstractDecision
    {
        var queryString = $"";

        if (limit is > 0)
        {
            queryString += $"limit={limit}";
            queryString += $"&";
        }

        queryString += $"submissionId={submissionId}&type={type}";

        return await webApiGatewayClient.GetDecisionsAsync<T>(queryString);
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

        return await webApiGatewayClient.GetSubmissionIdsAsync(organisationId, queryString);
    }

    public async Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime)
    {
        return await webApiGatewayClient.GetSubmissionHistoryAsync(submissionId, $"lastSyncTime={lastSyncTime:s}");
    }

    public async Task<bool> HasSubmissionsAsync(Guid organiationExternalId, SubmissionType type, Guid? complianceSchemeId)
    {
        var result = await GetSubmissionIdsAsync(organiationExternalId, type, complianceSchemeId, null);

        return (result is not null && result.Count > 0);
    }

    public async Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request)
    {
        return await webApiGatewayClient.GetRegistrationApplicationDetails(request);
    }
}