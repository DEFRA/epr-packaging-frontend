namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using System.Net.Http.Json;
using Constants;
using DTOs.ComplianceScheme;
using Extensions;
using Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Options;
using RequestModels;

public class ComplianceSchemeService : IComplianceSchemeService
{
    private const string AddComplianceSchemeErrorMessage = "Attempting to add compliance scheme failed";
    private const string UpdateComplianceSchemeErrorMessage = "Attempting to update compliance scheme failed";
    private const string StopComplianceSchemeErrorMessage = "Attempting to stop compliance scheme failed";
    private const string GetComplianceSchemeForProducerErrorMessage = "Attempting to get compliance scheme for producer failed";
    private const string GetAllComplianceSchemesErrorMessage = "Attempting to get all compliance schemes failed";

    private readonly ILogger<ComplianceSchemeService> _logger;
    private readonly bool _hasCache;
    private readonly IDistributedCache? _cache;
    private readonly DistributedCacheEntryOptions? _cacheEntryOptions;
    private readonly IAccountServiceApiClient _accountServiceApiClient;

    public ComplianceSchemeService(
        IAccountServiceApiClient accountServiceApiClient,
        ILogger<ComplianceSchemeService> logger,
        IOptions<CachingOptions> cachingIOptions,
        IDistributedCache? cache)
    {
        _logger = logger;
        _cache = cache;
        _accountServiceApiClient = accountServiceApiClient;

        var cachingOptions = cachingIOptions.Value;
        _hasCache = cache != null && cachingOptions.CacheComplianceSchemeSummaries;
        if (_hasCache)
        {
            _cache = cache;
            _cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(cachingOptions.SlidingExpirationSeconds))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(cachingOptions.AbsoluteExpirationSeconds));
        }
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> get/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/get .</FullLocalUrl>
    /// <summary>Gets all compliance schemes.</summary>
    /// <returns>IEnumerable.<ComplianceSchemeDto>?.</returns>
    public async Task<IEnumerable<ComplianceSchemeDto>?> GetComplianceSchemes()
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest(ComplianceSchemePaths.Get);
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<ComplianceSchemeDto>>(content).OrderBy(x => x.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetAllComplianceSchemesErrorMessage);
            throw;
        }
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> GetForProducer/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/GetForProducer .</FullLocalUrl>
    /// <summary>Gets a producer compliance scheme.</summary>
    /// <param name="producerOrganisationId">producer Organisation Identifier .</param>
    /// <returns>ProducerComplianceSchemeDto.</returns>
    public async Task<ProducerComplianceSchemeDto?> GetProducerComplianceScheme(Guid producerOrganisationId)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest(
                $"{ComplianceSchemePaths.GetForProducer}?producerOrganisationId={producerOrganisationId}");

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ProducerComplianceSchemeDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetComplianceSchemeForProducerErrorMessage);
            throw;
        }
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> GetForOperator/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/GetForOperator .</FullLocalUrl>
    /// <summary>Gets all compliance schemes for an operator.</summary>
    /// <param name="operatorOrganisationId">operator Organisation Identifier .</param>
    /// <returns> <list>ProducerComplianceSchemeDto</list> </returns>
    public async Task<List<ComplianceSchemeDto>> GetOperatorComplianceSchemes(Guid operatorOrganisationId)
    {
        try
        {
            var cacheKey = $"compliance-schemes-{operatorOrganisationId}";

            if (_hasCache && _cache.TryGetValue<List<ComplianceSchemeDto>>(cacheKey, out var summary))
            {
                return summary;
            }

            var result = await _accountServiceApiClient.SendGetRequest(
                $"{ComplianceSchemePaths.GetForOperator}?operatorOrganisationId={operatorOrganisationId}");
            result.EnsureSuccessStatusCode();

            var complianceSchemes = await result.Content.ReadFromJsonAsync<List<ComplianceSchemeDto>>();

            if (_hasCache)
            {
                await _cache.SetAsync(cacheKey, complianceSchemes, _cacheEntryOptions);
            }

            return complianceSchemes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetComplianceSchemeForProducerErrorMessage);
            throw;
        }
    }

    /// <endpoint>[facade]: /api/compliance-schemes/{complianceSchemeId}/summary.</endpoint>
    /// <summary>
    /// Gets cached compliance schemes stats for an operator organisation.
    /// </summary>
    /// <param name="organisationId">Operator organisation identifier.</param>
    /// <param name="complianceSchemeId">Compliance Scheme identifier.</param>
    /// <returns>List&lt;ComplianceSchemeSummary&gt;.</returns>
    public async Task<ComplianceSchemeSummary> GetComplianceSchemeSummary(Guid organisationId, Guid complianceSchemeId)
    {
        var cacheKey = SummaryCacheKey(organisationId, complianceSchemeId);

        if (_hasCache && _cache.TryGetValue<ComplianceSchemeSummary>(cacheKey, out var summary))
        {
            return summary;
        }

        var apiEndpoint = string.Format(ComplianceSchemePaths.Summary, complianceSchemeId);

        var result = await _accountServiceApiClient.SendGetRequest(organisationId, apiEndpoint);

        result.EnsureSuccessStatusCode();

        summary = await result.Content.ReadFromJsonAsync<ComplianceSchemeSummary>() ?? new ComplianceSchemeSummary();

        if (_hasCache)
        {
            await _cache.SetAsync(cacheKey, summary, _cacheEntryOptions);
        }

        return summary;
    }

    public async Task ClearSummaryCache(Guid organisationId, Guid complianceSchemeId)
    {
        if (!_hasCache)
        {
            return;
        }

        await _cache.RemoveAsync(SummaryCacheKey(organisationId, complianceSchemeId));
    }

    public bool HasCache()
    {
        return _hasCache;
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> Select/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/Select .</FullLocalUrl>
    /// <summary>Add a compliance scheme for a producer.</summary>
    /// <param name="complianceSchemeId">Compliance Scheme Identifier .</param>
    /// <param name="organisationId">Organisation Identifier .</param>
    /// <returns>SelectedSchemeDto.</returns>
    public async Task<SelectedSchemeDto> ConfirmAddComplianceScheme(Guid complianceSchemeId, Guid organisationId)
    {
        try
        {
            var selectedSchemeContent = new ComplianceSchemeServiceAddRequestModel
            {
                OrganisationId = organisationId,
                ComplianceSchemeId = complianceSchemeId,
            };
            var result = await _accountServiceApiClient.SendPostRequest(ComplianceSchemePaths.Select, selectedSchemeContent);
            var content = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<SelectedSchemeDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AddComplianceSchemeErrorMessage);
            throw;
        }
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> Update/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/Update .</FullLocalUrl>
    /// <summary>Update a compliance scheme for a producer.</summary>
    /// <param name="complianceSchemeId">Compliance Scheme Identifier .</param>
    /// <param name="selectedSchemeId">Selected Scheme Identifier .</param>
    /// <param name="producerOrganisationId">Producer Organisation Identifier .</param>
    /// <returns>SelectedSchemeDto.</returns>
    public async Task<SelectedSchemeDto> ConfirmUpdateComplianceScheme(Guid complianceSchemeId, Guid selectedSchemeId, Guid producerOrganisationId)
    {
        try
        {
            var selectedSchemeContent = new ComplianceSchemeServiceUpdateRequestModel
            {
                SelectedSchemeId = selectedSchemeId,
                OrganisationId = producerOrganisationId,
                ComplianceSchemeId = complianceSchemeId,
            };
            var result = await _accountServiceApiClient.SendPostRequest(ComplianceSchemePaths.Update, selectedSchemeContent);
            var content = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<SelectedSchemeDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, UpdateComplianceSchemeErrorMessage);
            throw;
        }
    }

    /// <LocalBaseURL> https://localhost:7253/api/compliance-scheme/ .</LocalBaseURL>
    /// <Endpoint> Remove/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/compliance-scheme/Remove .</FullLocalUrl>
    /// <summary>Remove/stop a compliance scheme for a producer.</summary>
    /// <param name="selectedSchemeId">SelectedSchemeId Scheme Identifier .</param>
    /// <param name="organisationId">Organisation Identifier .</param>
    /// <returns>HttpResponseMessage.</returns>
    public async Task<HttpResponseMessage> StopComplianceScheme(Guid selectedSchemeId, Guid organisationId)
    {
        try
        {
            var removeSchemeContent = new RemoveComplianceSchemeRequestModel
            {
                SelectedSchemeId = selectedSchemeId,
                OrganisationId = organisationId,
            };

            var result = await _accountServiceApiClient.SendPostRequest(ComplianceSchemePaths.Remove, removeSchemeContent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, StopComplianceSchemeErrorMessage);
            throw;
        }
    }

    private static string SummaryCacheKey(Guid organisationId, Guid complianceSchemeId)
    {
        return $"summary-{organisationId}-{complianceSchemeId}";
    }
}