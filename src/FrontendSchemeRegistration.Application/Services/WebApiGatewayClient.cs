using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace FrontendSchemeRegistration.Application.Services;

public class WebApiGatewayClient : ServiceClientBase, IWebApiGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly string[] _scopes;
    private readonly ILogger<WebApiGatewayClient> _logger;
    private readonly ITokenAcquisition _tokenAcquisition;

    public WebApiGatewayClient(
        HttpClient httpClient,
        ITokenAcquisition tokenAcquisition,
        IOptions<HttpClientOptions> httpClientOptions,
        IOptions<WebApiOptions> webApiOptions,
        ILogger<WebApiGatewayClient> logger)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;

        _scopes = [webApiOptions.Value.DownstreamScope];
        _httpClient.BaseAddress = new Uri(webApiOptions.Value.BaseEndpoint);
        _httpClient.AddHeaderUserAgent(httpClientOptions.Value.UserAgent);
        _httpClient.AddHeaderAcceptJson();
    }

    public async Task<Guid> UploadFileAsync(
        byte[] byteArray,
        string fileName,
        string submissionPeriod,
        Guid? submissionId,
        SubmissionType submissionType,
        SubmissionSubType? submissionSubType = null,
        Guid? registrationSetId = null,
        Guid? complianceSchemeId = null)
    {
        await PrepareAuthenticatedClientAsync();

        _httpClient.AddHeaderFileName(fileName);
        _httpClient.AddHeaderSubmissionType(submissionType);
        _httpClient.AddHeaderSubmissionSubTypeIfNotNull(submissionSubType);
        _httpClient.AddHeaderSubmissionIdIfNotNull(submissionId);
        _httpClient.AddHeaderSubmissionPeriod(submissionPeriod);
        _httpClient.AddHeaderRegistrationSetIdIfNotNull(registrationSetId);
        _httpClient.AddHeaderComplianceSchemeIdIfNotNull(complianceSchemeId);

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

    public async Task<T> GetDecisionsAsync<T>(string queryString)
        where T : AbstractDecision
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

    public async Task<List<SubsidiaryExportDto>> GetSubsidiariesAsync(int subsidiaryParentId)
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subsidiary/{subsidiaryParentId}");

            response.EnsureSuccessStatusCode();

            var subsidiaries = await response.Content.ReadFromJsonAsync<List<SubsidiaryExportDto>>();

            return subsidiaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subsidiaries");
            throw;
        }
    }

    public async Task<SubsidiaryFileUploadTemplateDto> GetSubsidiaryFileUploadTemplateAsync()
    {
        await PrepareAuthenticatedClientAsync();

        try
        {
            var response = await _httpClient.GetAsync("api/v1/file-upload-subsidiary/template");

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentDisposition?.FileName == null)
            {
                _logger.LogError("Failed to read subsidiary file upload template filename");

                return null;
            }

            if (response.Content.Headers.ContentType?.MediaType == null)
            {
                _logger.LogError("Failed to read subsidiary file upload template content type");

                return null;
            }

            return new SubsidiaryFileUploadTemplateDto
            {
                Name = response.Content.Headers.ContentDisposition.FileName,
                ContentType = response.Content.Headers.ContentType.MediaType,
                Content = await response.Content.ReadAsStreamAsync()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subsidiary file upload template");
            throw;
        }
    }

    // Gets all PRNs assigned to an organisation
    public async Task<List<PrnModel>> GetPrnsForLoggedOnUserAsync()
    {
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
		await PrepareAuthenticatedClientAsync();

		try
		{
			var response = await _httpClient.GetAsync($"/api/v1/prn/search{BuildUrlWithQueryString(request)}");

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

    private async Task PrepareAuthenticatedClientAsync()
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
           Microsoft.Identity.Web.Constants.Bearer, accessToken);
    }
}