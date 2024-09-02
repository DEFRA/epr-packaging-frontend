namespace FrontendSchemeRegistration.Application.Services;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using Interfaces;

[ExcludeFromCodeCoverage]
public class MockIntegrationServiceApiClient : IIntegrationServiceApiClient
{
    private readonly JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<HttpResponseMessage> SendGetRequest(string endpoint)
    {
        var index = endpoint.IndexOf("?id=");
        var companiesHouseNumber = index >= 0
            ? endpoint[(index + 4)..]
            : null;

        if (string.IsNullOrEmpty(companiesHouseNumber))
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var company = GetDummyCompany(companiesHouseNumber);
        var jsonContent = JsonSerializer.Serialize(company, options);

        response.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        return await Task.FromResult(response);
    }

    private static CompaniesHouseCompany GetDummyCompany(string companiesHouseNumber) =>
        new()
        {
            AccountExists = true,
            Organisation = new Organisation
            {
                Name = "Dummy Company",
                RegistrationNumber = companiesHouseNumber,
                RegisteredOffice = new RegisteredOfficeAddress
                {
                    BuildingNumber = "10",
                    BuildingName = "Dummy Place",
                    Street = "Dummy Street",
                    Town = "Nowhere",
                    Postcode = "AB1 0CD",
                    Country = new Country
                    {
                        Iso = "GB",
                        Name = "United Kingdom"
                    }
                }
            },
            AccountCreatedOn = companiesHouseNumber.Contains('X') ? DateTime.Now : null
        };
}