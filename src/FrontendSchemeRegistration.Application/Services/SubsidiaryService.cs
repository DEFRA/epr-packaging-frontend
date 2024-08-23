namespace FrontendSchemeRegistration.Application.Services;

using Constants;
using CsvHelper;
using CsvHelper.Configuration;
using DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.ClassMaps;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Net;

public class SubsidiaryService : ISubsidiaryService
{
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;

    public SubsidiaryService(IAccountServiceApiClient accountServiceApiClient, ILogger<SubsidiaryService> logger)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
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

    public async Task<Stream> GetSubsidiariesStreamAsync(int subsidiaryParentId, bool isComplienceScheme)
    {
        // This needs to be replaced with the call to Account Service during intigration

        var subsidiaries = await GetMockSubsidairyExportData(isComplienceScheme);

        var stream = new MemoryStream();

        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture)))
            {
                csv.Context.RegisterClassMap<ErrorReportRowMap>();
                await csv.WriteRecordsAsync(subsidiaries);
            }

            await writer.FlushAsync();
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<IList<SubsidiaryExportDto>> GetMockSubsidairyExportData(bool isComplienceScheme)
    {
        var subsidiaries = new List<SubsidiaryExportDto>();

        if (isComplienceScheme)
        {
            subsidiaries = new List<SubsidiaryExportDto>
            {
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 101, Organisation_Name = "Subsidiary A", Companies_House_Number = "CHN001" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 102, Organisation_Name = "Subsidiary B", Companies_House_Number = "CHN002" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 103, Organisation_Name = "Subsidiary C", Companies_House_Number = "CHN003" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 104, Organisation_Name = "Subsidiary D", Companies_House_Number = "CHN004" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 105, Organisation_Name = "Subsidiary E", Companies_House_Number = "CHN005" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 106, Organisation_Name = "Subsidiary F", Companies_House_Number = "CHN006" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 107, Organisation_Name = "Subsidiary G", Companies_House_Number = "CHN007" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 108, Organisation_Name = "Subsidiary H", Companies_House_Number = "CHN008" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 109, Organisation_Name = "Subsidiary I", Companies_House_Number = "CHN009" },
            new SubsidiaryExportDto { Organisation_Id = 200, Subsidiary_Id = 110, Organisation_Name = "Subsidiary J", Companies_House_Number = "CHN010" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 101, Organisation_Name = "Subsidiary A", Companies_House_Number = "CHN001" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 202, Organisation_Name = "Subsidiary B", Companies_House_Number = "CHN002" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 303, Organisation_Name = "Subsidiary C", Companies_House_Number = "CHN003" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 304, Organisation_Name = "Subsidiary D", Companies_House_Number = "CHN004" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 305, Organisation_Name = "Subsidiary E", Companies_House_Number = "CHN005" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 306, Organisation_Name = "Subsidiary F", Companies_House_Number = "CHN006" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 307, Organisation_Name = "Subsidiary G", Companies_House_Number = "CHN007" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 308, Organisation_Name = "Subsidiary H", Companies_House_Number = "CHN008" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 309, Organisation_Name = "Subsidiary I", Companies_House_Number = "CHN009" },
            new SubsidiaryExportDto { Organisation_Id = 300, Subsidiary_Id = 310, Organisation_Name = "Subsidiary J", Companies_House_Number = "CHN010" }
            };
        }
        else
        {
            subsidiaries = new List<SubsidiaryExportDto>
            {
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 101, Organisation_Name = "Subsidiary A", Companies_House_Number = "CHN001" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 102, Organisation_Name = "Subsidiary B", Companies_House_Number = "CHN002" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 103, Organisation_Name = "Subsidiary C", Companies_House_Number = "CHN003" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 104, Organisation_Name = "Subsidiary D", Companies_House_Number = "CHN004" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 105, Organisation_Name = "Subsidiary E", Companies_House_Number = "CHN005" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 106, Organisation_Name = "Subsidiary F", Companies_House_Number = "CHN006" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 107, Organisation_Name = "Subsidiary G", Companies_House_Number = "CHN007" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 108, Organisation_Name = "Subsidiary H", Companies_House_Number = "CHN008" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 109, Organisation_Name = "Subsidiary I", Companies_House_Number = "CHN009" },
            new SubsidiaryExportDto { Organisation_Id = 100, Subsidiary_Id = 110, Organisation_Name = "Subsidiary J", Companies_House_Number = "CHN010" }
            };
        }

        return subsidiaries;
    }
}