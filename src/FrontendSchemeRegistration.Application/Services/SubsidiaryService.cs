namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using Constants;
using DTOs.Subsidiary;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
}