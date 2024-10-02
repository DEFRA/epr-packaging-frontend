namespace FrontendSchemeRegistration.Application.Services;

using System.Globalization;
using System.IO;
using System.Net;
using ClassMaps;
using Constants;
using CsvHelper;
using CsvHelper.Configuration;
using DTOs;
using DTOs.Subsidiary;
using DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class SubsidiaryService : ISubsidiaryService
{
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;
    private readonly IWebApiGatewayClient _webApiGatewayClient;

    public SubsidiaryService(IAccountServiceApiClient accountServiceApiClient, IWebApiGatewayClient webApiGatewayClient, ILogger<SubsidiaryService> logger)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
        _webApiGatewayClient = webApiGatewayClient;
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

    public async Task<Stream> GetSubsidiariesStreamAsync(Guid organisationId, Guid? complianceSchemeId, bool isComplianceScheme)
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
                csv.Context.RegisterClassMap<ExportOrganisationSubsidiariesRowMap>();
                csv.Context.RegisterClassMap<ErrorReportRowMap>();
                await csv.WriteRecordsAsync(subsidiaries);
            }

            await writer.FlushAsync();
        }

        stream.Position = 0;
        return stream;
    }

    public async Task<SubsidiaryFileUploadTemplateDto> GetFileUploadTemplateAsync()
    {
        return await _webApiGatewayClient.GetSubsidiaryFileUploadTemplateAsync();
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
}