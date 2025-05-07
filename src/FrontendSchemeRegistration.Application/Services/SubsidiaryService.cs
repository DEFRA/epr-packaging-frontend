namespace FrontendSchemeRegistration.Application.Services;

using System.Globalization;
using System.IO;
using System.Net;
using ClassMaps;
using Constants;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DTOs.Subsidiary;
using DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.Application.Helpers;
using FrontendSchemeRegistration.Application.DTOs;
using System.Linq;
using System.Web;

public class SubsidiaryService : ISubsidiaryService
{
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;
    private readonly IWebApiGatewayClient _webApiGatewayClient;
    private const string RedisFileUploadStatusViewedKey = "SubsidiaryFileUploadStatusViewed";
    private readonly IDistributedCache _distributedCache;
    private readonly DistributedCacheEntryOptions _cacheEntryOptions;
    private const string OrganisationByCompanyHouseNumberUrl = "organisations/organisation-by-company-house-number";

    public SubsidiaryService(
        IAccountServiceApiClient accountServiceApiClient,
        IWebApiGatewayClient webApiGatewayClient,
        ILogger<SubsidiaryService> logger,
        IDistributedCache distributedCache)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
        _webApiGatewayClient = webApiGatewayClient;
        _distributedCache = distributedCache;
        _cacheEntryOptions = new DistributedCacheEntryOptions();
    }

    public async Task SetSubsidiaryFileUploadStatusViewedAsync(bool value, Guid userId, Guid organisationId)
    {
        var redisKey = $"{RedisFileUploadStatusViewedKey}:{userId}:{organisationId}";
        await _distributedCache.SetAsync(redisKey, value, _cacheEntryOptions);
    }

    public async Task<bool> GetSubsidiaryFileUploadStatusViewedAsync(Guid userId, Guid organisationId)
    {
        var redisKey = $"{RedisFileUploadStatusViewedKey}:{userId}:{organisationId}";
        if (_distributedCache.TryGetValue<bool>(redisKey, out var value))
        {
            return value;
        }
        return false;
    }

    public async Task<string> SaveSubsidiary(SubsidiaryDto subsidiary)
    {
        try
        {
            var result = await _accountServiceApiClient.SendPostRequest(SubsidiaryPaths.CreateAndAddSubsidiary, subsidiary);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<string>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save subsidiary");
            throw;
        }
    }

    public async Task<string> AddSubsidiary(SubsidiaryAddDto subsidiary)
    {
        try
        {
            var result = await _accountServiceApiClient.SendPostRequest(SubsidiaryPaths.AddSubsidiary, subsidiary);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<string>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save subsidiary");
            throw;
        }
    }

    public async Task<OrganisationDto> GetOrganisationByReferenceNumber(string referenceNumber)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest($"organisations/organisation-by-reference-number/{WebUtility.UrlEncode(referenceNumber)}");
            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OrganisationDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subsidiary data");
            throw;
        }
    }

    public async Task<OrganisationDto> GetOrganisationParent(string referenceNumber)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest($"organisations/organisation-parent-details/{WebUtility.UrlEncode(referenceNumber)}");
            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OrganisationDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subsidiary data");
            throw;
        }
    }

    public async Task<OrganisationDto> GetOrganisationsByCompaniesHouseNumber(string companyHouseNumber)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest($"{OrganisationByCompanyHouseNumberUrl}/{WebUtility.UrlEncode(companyHouseNumber)}");
            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OrganisationDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subsidiary data");
            throw;
        }
    }
    
    public async Task<Stream?> GetAllSubsidiariesStream()
    {
        try
        {
            var listOfAllSubsidiaries = await GetUnpagedOrganisationSubsidiaries();

            var stream = new MemoryStream();

            await using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);

                    csv.Context.RegisterClassMap(new ExportOrganisationAllSubsidiariesRowMap());
                    await csv.WriteRecordsAsync(listOfAllSubsidiaries);
                }

                await writer.FlushAsync();
            }

            stream.Position = 0;
            return stream;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task<PaginatedResponse<RelationshipResponseModel>> GetPagedOrganisationSubsidiaries(int page, int showPerPage, string searchTerm = null)
    {
        try
        {
            var url = $"organisations/organisationRelationships?page={page}&showPerPage={showPerPage}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&search={HttpUtility.UrlEncode(searchTerm)}";
            }

            var result = await _accountServiceApiClient.SendGetRequest(url);

            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PaginatedResponse<RelationshipResponseModel>>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all subsidiary data");
            throw;
        }
    }

    public async Task<List<RelationshipResponseModel>> GetUnpagedOrganisationSubsidiaries()
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest($"organisations/organisationRelationshipsWithoutPaging");
            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RelationshipResponseModel>>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unpaged subsidiary data");
            throw;
        }
    }

    public async Task<OrganisationRelationshipModel> GetOrganisationSubsidiaries(Guid organisationId)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest($"organisations/{organisationId}/organisationRelationships");
            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OrganisationRelationshipModel>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subsidiary data");
            throw;
        }
    }

    public async Task<Stream> GetSubsidiariesStreamAsync(Guid organisationId, Guid? complianceSchemeId, bool isComplianceScheme, bool includeSubsidiaryJoinerAndLeaverColumns)
    {
        HttpResponseMessage result;
        if (isComplianceScheme)
        {
            result = await _accountServiceApiClient.SendGetRequest($"compliance-schemes/{organisationId}/schemes/{complianceSchemeId}/export-subsidiaries");
            result.EnsureSuccessStatusCode();
        }
        else
        {
            result = await _accountServiceApiClient.SendGetRequest($"organisations/{organisationId}/export-subsidiaries");
            result.EnsureSuccessStatusCode();
        }

        var content = await result.Content.ReadAsStringAsync();
        var subsidiaries = JsonConvert.DeserializeObject<List<ExportOrganisationSubsidiariesResponseModel>>(content);

        var stream = new MemoryStream();

        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);

                csv.Context.RegisterClassMap(new ExportOrganisationSubsidiariesRowMap(includeSubsidiaryJoinerAndLeaverColumns));
                await csv.WriteRecordsAsync(subsidiaries);
            }

            await writer.FlushAsync();

        }

        stream.Position = 0;
        return stream;
    }

    public async Task<SubsidiaryFileUploadStatus> GetSubsidiaryFileUploadStatusAsync(Guid userId, Guid organisationId)
    {
        var response = await _webApiGatewayClient.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        if (string.IsNullOrWhiteSpace(response.Status))
        {
            return SubsidiaryFileUploadStatus.NoFileUploadActive;
        }

        if (response.Status.Equals("finished", StringComparison.OrdinalIgnoreCase))
        {
            if (response.RowsAdded > 0 && response.Errors?.Count > 0)
            {
                return SubsidiaryFileUploadStatus.PartialUpload;
            }

            return response.Errors == null
                ? SubsidiaryFileUploadStatus.FileUploadedSuccessfully
                : SubsidiaryFileUploadStatus.HasErrors;
        }

        if (response.Status.Equals("uploading", StringComparison.OrdinalIgnoreCase))
        {
            return SubsidiaryFileUploadStatus.FileUploadInProgress;
        }

        return SubsidiaryFileUploadStatus.NoFileUploadActive;
    }

    public async Task TerminateSubsidiary(Guid parentOrganisationExternalId, Guid childOrganisationId, Guid userId)
    {
        try
        {
            var result = await _accountServiceApiClient.SendPostRequest($"organisations/terminate-subsidiary", new
            {
                ParentOrganisationId = parentOrganisationExternalId,
                ChildOrganisationId = childOrganisationId,
                UserId = userId
            });

            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate subsidiary");
            throw;
        }
    }

    public async Task<SubsidiaryUploadStatusDto> GetUploadStatus(Guid userId, Guid organisationId)
    {
        return await _webApiGatewayClient.GetSubsidiaryUploadStatus(userId, organisationId);
    }

    public async Task<Stream> GetUploadErrorsReport(Guid userId, Guid organisationId)
    {
        var uploadStatus = await _webApiGatewayClient.GetSubsidiaryUploadStatus(userId, organisationId);

        var errorRows = new List<SubsidiaryUploadErrorRow>();

        if (uploadStatus.Errors != null)
        {
            foreach (var error in uploadStatus.Errors)
            {
                var rowContent = error.FileContent.Replace("\r\n", "").Split(',');

                errorRows.Add(new SubsidiaryUploadErrorRow
                {
                    OrganisationId = rowContent.ElementAtOrDefault(0),
                    SubsidiaryId = rowContent.ElementAtOrDefault(1),
                    OrganisationName = rowContent.ElementAtOrDefault(2),
                    CompaniesHouseNumber = rowContent.ElementAtOrDefault(3),
                    ParentChild = rowContent.ElementAtOrDefault(4),
                    FranchiseeLicenseeTenant = rowContent.ElementAtOrDefault(5),
                    RowNumber = error.FileLineNumber,
                    Issue = error.IsError ? "Error" : "Warning",
                    Message = error.Message
                });
            }
        }

        var stream = new MemoryStream();
        BomHelper.PrependBOMBytes(stream);

        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Context.RegisterClassMap<SubsidiaryUploadErrorRowMap>();

                await csv.WriteRecordsAsync(errorRows);
            }

            await writer.FlushAsync();
        }

        stream.Position = 0;

        return stream;
    }
}