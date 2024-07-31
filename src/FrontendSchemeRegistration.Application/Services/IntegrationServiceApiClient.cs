namespace FrontendSchemeRegistration.Application.Services;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Extensions;
using Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Options;

[ExcludeFromCodeCoverage]
public class IntegrationServiceApiClient : IIntegrationServiceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string[] _scopes;
    private readonly ITokenAcquisition _tokenAcquisition;

    public IntegrationServiceApiClient(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IOptions<AccountsFacadeApiOptions> options)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _scopes = new[] { options.Value.DownstreamScope };
    }

    public async Task<HttpResponseMessage> SendGetRequest(string endpoint)
    {
        await PrepareAuthenticatedClient();
        return await _httpClient.GetAsync(endpoint);
    }

    private async Task PrepareAuthenticatedClient()
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        _httpClient.AddHeaderAcceptJson();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Bearer, accessToken);
    }
}