namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DTOs;
using DTOs.Submission;
using Enums;
using Extensions;
using Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Options;

public class WebApiGatewayClient : IWebApiGatewayClient
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

        _scopes = new[] { webApiOptions.Value.DownstreamScope };
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
        return new Guid(responseLocation.Split('/').Last());
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

    private async Task PrepareAuthenticatedClientAsync()
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Bearer, accessToken);
    }
}