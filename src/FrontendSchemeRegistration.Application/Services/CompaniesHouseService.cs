namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using System.Net.Http.Json;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using Interfaces;
using Microsoft.Extensions.Logging;

public class CompaniesHouseService : ICompaniesHouseService
{
    private const string GetCompaniesHouseErrorMessage = "Attempting to get companies house information failed";
    private readonly ILogger<CompaniesHouseService> _logger;
    private readonly IIntegrationServiceApiClient _integrationServiceApiClient;

    public CompaniesHouseService(IIntegrationServiceApiClient integrationServiceApiClient, ILogger<CompaniesHouseService> logger)
    {
        _logger = logger;
        _integrationServiceApiClient = integrationServiceApiClient;
    }

    public async Task<Company?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber)
    {
        try
        {
            var path = $"companies-house?id={companiesHouseNumber}";

            var response = await _integrationServiceApiClient.SendGetRequest(path);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var company = await response.Content.ReadFromJsonAsync<CompaniesHouseCompany>();

            return new Company(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetCompaniesHouseErrorMessage);
            throw;
        }
    }
}