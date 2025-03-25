using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace FrontendSchemeRegistration.Application.Services;

public class WebApiGatewayClient : IWebApiGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly string[] _scopes;
    private readonly ILogger<WebApiGatewayClient> _logger;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IComplianceSchemeMemberService _complianceSchemeSvc;
    private readonly List<HttpStatusCode> PassThroughExceptions;

    public WebApiGatewayClient(
        HttpClient httpClient,
        ITokenAcquisition tokenAcquisition,

        IOptions<HttpClientOptions> httpClientOptions,
        IOptions<WebApiOptions> webApiOptions,
        ILogger<WebApiGatewayClient> logger,
        IComplianceSchemeMemberService complianceSchemeSvc)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
        _complianceSchemeSvc = complianceSchemeSvc;
        _scopes = [webApiOptions.Value.DownstreamScope];
        _httpClient.BaseAddress = new Uri(webApiOptions.Value.BaseEndpoint);
        _httpClient.AddHeaderUserAgent(httpClientOptions.Value.UserAgent);
        _httpClient.AddHeaderAcceptJson();
        PassThroughExceptions ??= new List<HttpStatusCode> { HttpStatusCode.PreconditionRequired };
    }

    public async Task<Guid> UploadFileAsync(
        byte[] byteArray,
        string fileName,
        string submissionPeriod,
        Guid? submissionId,
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null,
        bool? isResubmission = null)
    {
        await PrepareAuthenticatedClientAsync();

        _httpClient.AddHeaderFileName(fileName);
        _httpClient.AddHeaderSubmissionType(submissionType);
        _httpClient.AddHeaderSubmissionSubTypeIfNotNull(submissionSubType);
        _httpClient.AddHeaderSubmissionIdIfNotNull(submissionId);
        _httpClient.AddHeaderSubmissionPeriod(submissionPeriod);
        _httpClient.AddHeaderRegistrationSetIdIfNotNull(registrationSetId);
        _httpClient.AddHeaderComplianceSchemeIdIfNotNull(complianceSchemeId);
        _httpClient.AddHeaderIsResubmissionIfNotNull(isResubmission);

        var response = await _httpClient.PostAsync("api/v1/file-upload", new ByteArrayContent(byteArray));

        response.EnsureSuccessStatusCode();
        var responseLocation = response.Headers.Location.ToString();
        var parts = responseLocation.Split('/');
        return new Guid(parts[^1]);
    }

    public async Task<Guid> UploadSubsidiaryFileAsync(
       byte[] byteArray,
       string fileName,
       Guid? submissionId,
       SubmissionType submissionType,
       Guid? complianceSchemeId = null)
    {
        await PrepareAuthenticatedClientAsync();

        _httpClient.AddHeaderFileName(fileName);
        _httpClient.AddHeaderSubmissionType(submissionType);
        _httpClient.AddHeaderSubmissionIdIfNotNull(submissionId);
        _httpClient.AddHeaderComplianceSchemeIdIfNotNull(complianceSchemeId);

        var response = await _httpClient.PostAsync("api/v1/file-upload-subsidiary", new ByteArrayContent(byteArray));

        response.EnsureSuccessStatusCode();
        var responseLocation = response.Headers.Location.ToString();
        return new Guid(responseLocation.Split('/')[^1]);
    }

    public async Task<List<T>> GetSubmissionsAsync<T>(string queryString)
        where T : AbstractSubmission
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/submissions?{queryString}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<T>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions");
            throw;
        }
    }

    public async Task<T?> GetSubmissionAsync<T>(Guid id)
        where T : AbstractSubmission
    {
        try
        {
            await PrepareAuthenticatedClientAsync();

            var response = await _httpClient.GetAsync($"/api/v1/submissions/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission {Id}", id);
            throw;
        }
    }

    public async Task<List<ProducerValidationError>> GetProducerValidationErrorsAsync(Guid submissionId)
    {
        try
        {
            await PrepareAuthenticatedClientAsync();

            var requestPath = $"/api/v1/submissions/{submissionId}/producer-validations";

            var response = await _httpClient.GetAsync(requestPath);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ProducerValidationError>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting producer validation records with submissionId: {Id}", submissionId);
            throw;
        }
    }

    public async Task<List<RegistrationValidationError>> GetRegistrationValidationErrorsAsync(Guid submissionId)
    {
        try
        {
            await PrepareAuthenticatedClientAsync();

            var requestPath = $"/api/v1/submissions/{submissionId}/organisation-details-errors";

            var response = await _httpClient.GetAsync(requestPath);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<RegistrationValidationError>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration validation records with submissionId: {Id}", submissionId);
            throw;
        }
    }

    public async Task SubmitAsync(Guid submissionId, SubmissionPayload payload)
    {
        await PrepareAuthenticatedClientAsync();
        var requestPath = $"/api/v1/submissions/{submissionId}/submit";
        var response = await _httpClient.PostAsJsonAsync(requestPath, payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateRegistrationApplicationEvent(Guid submissionId, RegistrationApplicationPayload applicationPayload)
    {
        await PrepareAuthenticatedClientAsync();
        var requestPath = $"/api/v1/submissions/{submissionId}/submit-registration-application";
        var response = await _httpClient.PostAsJsonAsync(requestPath, applicationPayload);
        response.EnsureSuccessStatusCode();
    }
    
    public async Task<T> GetDecisionsAsync<T>(string queryString) where T : AbstractDecision
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/decisions?{queryString}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decision");
            throw;
        }
    }

    public async Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, string queryString)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/submissions/submission-Ids/{organisationId}?{queryString}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<SubmissionPeriodId>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission ids");
            throw;
        }
    }

    public async Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, string queryString)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/submissions/submission-history/{submissionId}?{queryString}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<SubmissionHistory>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission history");
            throw;
        }
    }

    // Gets all PRNs assigned to an organisation
    public async Task<List<PrnModel>> GetPrnsForLoggedOnUserAsync()
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/prn/organisation");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<PrnModel>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recycling notes for organisation");
            throw;
        }
    }

    public async Task<PaginatedResponse<PrnModel>> GetSearchPrnsAsync(PaginatedRequest request)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/prn/search{ServiceClientBase.BuildUrlWithQueryString(request)}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<PrnModel>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recycling notes for organisation");
            throw;
        }

    }

    // Get single PRN
    public async Task<PrnModel> GetPrnByExternalIdAsync(Guid id)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/prn/{id}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PrnModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recycling note {id}", id);
            throw;
        }
    }

    public async Task SetPrnApprovalStatusToAcceptedAsync(Guid id)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            UpdatePrnStatus prnStatus = new() { PrnId = id, Status = PrnStatus.Accepted };
            List<UpdatePrnStatus> payload = new() { prnStatus };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/prn/status", payload);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting recycling note {id}", id);
            throw;
        }
    }

    public async Task SetPrnApprovalStatusToAcceptedAsync(Guid[] ids)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();
        try
        {
            var payload = ids.Select(x => new UpdatePrnStatus() { PrnId = x, Status = PrnStatus.Accepted });
            var response = await _httpClient.PostAsJsonAsync("/api/v1/prn/status", payload);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting recycling note {ids}", ids);
            throw;
        }
    }

    public async Task SetPrnApprovalStatusToRejectedAsync(Guid id)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            UpdatePrnStatus prnStatus = new() { PrnId = id, Status = PrnStatus.Rejected };
            List<UpdatePrnStatus> payload = new() { prnStatus };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/prn/status", payload);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting recycling note {id}", id);
            throw;
        }
    }

    public async Task<UploadFileErrorResponse> GetSubsidiaryFileUploadStatusAsync(Guid userId, Guid organisationId)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subsidiary/{userId}/{organisationId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UploadFileErrorResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subsidiary file upload status for user {UserId} and organisation {OrganisationId}", userId, organisationId);
            throw;
        }
    }
    public async Task<SubsidiaryUploadStatusDto> GetSubsidiaryUploadStatus(Guid userId, Guid organisationId)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subsidiary/{userId}/{organisationId}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<SubsidiaryUploadStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subsidiary upload validation result");
            throw;
        }
    }

    public async Task<PrnObligationModel> GetRecyclingObligationsCalculation(List<Guid> externalIds, int year)
    {
        AddComplianceSchemeHeader();
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/prn/obligationcalculation/{year}", externalIds);

			response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PrnObligationModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recycling obligations for organisation");
            throw;
        }
    }

    public async Task<RegistrationApplicationDetails?> GetRegistrationApplicationDetails(GetRegistrationApplicationDetailsRequest request)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var endpointUrl = $"/api/v1/registration/get-registration-application-details" +
                              $"?OrganisationNumber={request.OrganisationNumber}" +
                              $"&OrganisationId={request.OrganisationId}" +
                              $"&SubmissionPeriod={request.SubmissionPeriod}" +
                              $"&LateFeeDeadline={request.LateFeeDeadline:yyyy/MM/dd}";

            if (request.ComplianceSchemeId is not null && request.ComplianceSchemeId != Guid.Empty)
            {
                endpointUrl += $"&ComplianceSchemeId={request.ComplianceSchemeId}";
            }

            var response = await _httpClient.GetAsync(endpointUrl);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<RegistrationApplicationDetails>())!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Getting Registration Application Submission Details for organisation Id : {organisationId}", request.OrganisationId);
            return null!;
        }
    }

    public async Task<PackagingResubmissionApplicationDetails?> GetPackagingDataResubmissionApplicationDetails(GetPackagingResubmissionApplicationDetailsRequest request)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var endpointUrl = $"/api/v1/packaging-resubmission/get-application-details?OrganisationNumber={request.OrganisationNumber}&OrganisationId={request.OrganisationId}&SubmissionPeriod={request.SubmissionPeriod}";

            if (request.ComplianceSchemeId is not null && request.ComplianceSchemeId != Guid.Empty)
            {
                endpointUrl += $"&ComplianceSchemeId={request.ComplianceSchemeId}";
            }

            var response = await _httpClient.GetAsync(endpointUrl);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<PackagingResubmissionApplicationDetails>())!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Getting Pom Resubmission ApplicationDetails for organisation Id : {organisationId}", request.OrganisationId);
            return null!;
        }
    }

    public async Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var endpointUrl = $"/api/v1/packaging-resubmission/get-resubmission-member-details/{request.SubmissionId}?ComplianceSchemeId={request.ComplianceSchemeId}";

            var response = await _httpClient.GetAsync(endpointUrl);

            response.EnsureSuccessStatusCodeDoNotConsumeExceptions();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<PackagingResubmissionMemberDetails>())!;
        }
        catch (Exception ex)
        {
            if(ex.GetType() == typeof(HttpRequestException))
            {
                var requestException = ex as HttpRequestException;
                if (requestException.StatusCode.HasValue && PassThroughExceptions.Contains(requestException.StatusCode.Value))
                {
                    throw;
                }
            }    

            _logger.LogError(ex, "Error Getting Pom Resubmission MemberDetails");
            return null!;
        }
    }

    public async Task<byte[]> FileDownloadAsync(string queryString)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var fileResponse = await _httpClient.GetAsync($"api/v1/file-download?{queryString}");

            fileResponse.EnsureSuccessStatusCode();

            var fileData = await fileResponse.Content.ReadAsByteArrayAsync();

            return fileData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Downloading File");
            throw;
        }
    }

	public async Task CreatePackagingResubmissionReferenceNumberEvent(Guid submissionId, PackagingResubmissionReferenceNumberCreatedEvent @event)
	{
		await PrepareAuthenticatedClientAsync();
		var requestPath = $"/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-reference-number-event";
		var response = await _httpClient.PostAsJsonAsync(requestPath, @event);
		response.EnsureSuccessStatusCode();
	}

    public async Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId)
    {
        var eventPayload = new PackagingResubmissionFeeViewCreatedEvent() { IsPackagingResubmissionFeeViewed = true };
        await PrepareAuthenticatedClientAsync();
        var requestPath = $"/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-view-event";
        var response = await _httpClient.PostAsJsonAsync(requestPath, eventPayload);
        response.EnsureSuccessStatusCode();

    }

    public async Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod)
    {
        var eventPayload = new PackagingDataResubmissionFeePaymentEvent() { FileId = filedId, PaidAmount ="0", PaymentMethod = paymentMethod, PaymentStatus = "null" };
        await PrepareAuthenticatedClientAsync();
        var requestPath = $"/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-payment-event";
        var response = await _httpClient.PostAsJsonAsync(requestPath, eventPayload);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment)
    {
        var eventPayload = new PackagingResubmissionApplicationSubmittedCreatedEvent() { Comments = comment,FileId = filedId, IsResubmitted = true, SubmissionDate = submissionDate, SubmittedBy = submittedBy };
        await PrepareAuthenticatedClientAsync();
        var requestPath = $"/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-application-submitted-event";
        var response = await _httpClient.PostAsJsonAsync(requestPath, eventPayload);
        response.EnsureSuccessStatusCode();
    }

    private async Task PrepareAuthenticatedClientAsync()
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
           Microsoft.Identity.Web.Constants.Bearer, accessToken);
    }

    private void AddComplianceSchemeHeader()
    {
        var complianceSchemeId = _complianceSchemeSvc.GetComplianceSchemeId();
        _httpClient.AddHeaderComplianceSchemeIdIfNotNull(complianceSchemeId);
    }
}