namespace FrontendSchemeRegistration.Application.Services;

using DTOs;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class RoleManagementService : IRoleManagementService
{
    private const string GetDelegatedPersonNominatorErrorMessage = "Attempting to get Delegated Person Nominator failed";
    private readonly ILogger<RoleManagementService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;

    public RoleManagementService(IAccountServiceApiClient accountServiceApiClient, ILogger<RoleManagementService> logger)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
    }

    public async Task<DelegatedPersonNominatorDto> GetDelegatedPersonNominator(Guid enrolmentId, Guid? organisationId)
    {
        try
        {
            _accountServiceApiClient.AddHttpClientHeader("X-EPR-Organisation", organisationId.ToString());
            var result = await _accountServiceApiClient.SendGetRequest($"enrolments/{enrolmentId}/delegated-person-nominator?serviceKey=Packaging");

            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DelegatedPersonNominatorDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetDelegatedPersonNominatorErrorMessage);
            throw;
        }
    }

    public async Task<HttpResponseMessage> AcceptNominationToDelegatedPerson(Guid enrolmentId, Guid organisationId, string serviceKey, AcceptNominationRequest acceptNominationRequest)
    {
        var endpoint = $"enrolments/{enrolmentId}/delegated-person-acceptance?serviceKey={serviceKey}";

        var response = await _accountServiceApiClient.PutAsJsonAsync<AcceptNominationRequest>(organisationId, endpoint, acceptNominationRequest);

        response.EnsureSuccessStatusCode();

        return response;
    }

    public async Task<HttpResponseMessage> AcceptNominationToApprovedPerson(Guid enrolmentId, Guid organisationId, string serviceKey, AcceptApprovedPersonRequest acceptApprovedPersonRequest)
    {
        var endpoint = $"enrolments/{enrolmentId}/approved-person-acceptance?serviceKey={serviceKey}";

        var response = await _accountServiceApiClient.PutAsJsonAsync<AcceptApprovedPersonRequest>(organisationId, endpoint, acceptApprovedPersonRequest);

        response.EnsureSuccessStatusCode();

        return response;
    }
}
