namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using Constants;
using DTOs.UserAccount;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class UserAccountService : IUserAccountService
{
    private const string GetUserAccountErrorMessage = "Attempting to get user account failed";
    private readonly ILogger<UserAccountService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;

    public UserAccountService(IAccountServiceApiClient accountServiceApiClient, ILogger<UserAccountService> logger)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
    }

    /// <LocalBaseURL> https://localhost:7253/api/ .</LocalBaseURL>
    /// <Endpoint> user-accounts/ .</Endpoint>
    /// <FullLocalUrl> https://localhost:7253/api/user-accounts .</FullLocalUrl>
    /// <summary>Gets a users account.</summary>
    /// <returns>UserAccountDto.</returns>
    public async Task<UserAccountDto?> GetUserAccount()
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest(UserAccountPaths.Get);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserAccountDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetUserAccountErrorMessage);
            throw;
        }
    }

    public async Task<PersonDto> GetPersonByUserId(Guid userId)
    {
        try
        {
            var result = await _accountServiceApiClient.SendGetRequest(string.Format(UserAccountPaths.GetPersonByUserId, userId));
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PersonDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetUserAccountErrorMessage);
            throw;
        }
    }
}