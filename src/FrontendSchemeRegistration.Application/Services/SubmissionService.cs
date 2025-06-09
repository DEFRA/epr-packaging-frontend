using System.Web;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.DTOs.UserAccount;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FrontendSchemeRegistration.Application.Services;

public class SubmissionService(IWebApiGatewayClient webApiGatewayClient) : ISubmissionService
{
    public async Task<T> GetSubmissionAsync<T>(Guid submissionId) where T : AbstractSubmission
    {
        return await webApiGatewayClient.GetSubmissionAsync<T>(submissionId);
    }

    public async Task<List<T>> GetSubmissionsAsync<T>(List<string> periods, int? limit, Guid? complianceSchemeId) where T : AbstractSubmission
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
        await SubmitAsync(submissionId, fileId, null, null, false);
    }

    public async Task SubmitAsync(Guid submissionId, Guid fileId, string? submittedBy, string? appReferenceNumber = null, bool? isResubmitted = null)
    {
        var payload = new SubmissionPayload
        {
            FileId = fileId,
            SubmittedBy = submittedBy,
            AppReferenceNumber = appReferenceNumber,
            IsResubmission = isResubmitted
        };

        await webApiGatewayClient.SubmitAsync(submissionId, payload);
    }

    public async Task CreateRegistrationApplicationEvent(Guid submissionId, Guid? complianceSchemeId, string? comments, string? paymentMethod, string applicationReferenceNumber, bool isResubmission, SubmissionType submissionType)
    {
        var applicationPayload = new RegistrationApplicationPayload
        {
            ApplicationReferenceNumber = applicationReferenceNumber,
            ComplianceSchemeId = complianceSchemeId,
            PaymentMethod = paymentMethod ?? string.Empty,
            PaymentStatus = "Not-Applicable",
            PaidAmount = "0",
            Comments = comments,
            SubmissionType = submissionType,
            IsResubmission = isResubmission
        };
        await webApiGatewayClient.CreateRegistrationApplicationEvent(submissionId, applicationPayload);
    }

    public async Task<T> GetDecisionAsync<T>(int? limit, Guid submissionId, SubmissionType type) where T : AbstractDecision
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

    public async Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request)
    {
        return await webApiGatewayClient.GetRegistrationApplicationDetails(request);
    }

    public async Task<List<PackagingResubmissionApplicationDetails>?> GetPackagingDataResubmissionApplicationDetails(GetPackagingResubmissionApplicationDetailsRequest request)
    {
        return await webApiGatewayClient.GetPackagingDataResubmissionApplicationDetails(request);
    }

    public async Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request)
    {
        return await webApiGatewayClient.GetPackagingResubmissionMemberDetails(request);
    }

	public async Task CreatePackagingResubmissionReferenceNumberEvent(Guid submissionId, PackagingResubmissionReferenceNumberCreatedEvent @event)
	{
		await webApiGatewayClient.CreatePackagingResubmissionReferenceNumberEvent(submissionId, @event);
	}

    public async Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId, Guid? filedId)
    {
        await webApiGatewayClient.CreatePackagingResubmissionFeeViewEvent(submissionId, filedId);
    }

    public async Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod)
    {
        await webApiGatewayClient.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId, paymentMethod);
    }

    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment)
    {
        await webApiGatewayClient.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId, submittedBy, submissionDate, comment);
    }

    public async Task<bool> IsAnySubmissionAcceptedForDataPeriod(PomSubmission submission, Guid organisationId, Guid? complienceSchemaId)
    {
        var submissionIds = await GetSubmissionIdsAsync(
            organisationId,
            SubmissionType.Producer,
            complienceSchemaId,
            null);
        if (submissionIds != null && submissionIds.Count > 0)
        {
            var submissionId = submissionIds.Find(x => x.SubmissionId == submission.Id);
            var submissionHistory = await GetSubmissionHistoryAsync(
                                           submission.Id,
                                           new DateTime(submissionId.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            if (submissionHistory != null && submissionHistory.Exists(s => s.Status == "Accepted"))
            {
                return true;
            }
        }
        return false;
    }
}