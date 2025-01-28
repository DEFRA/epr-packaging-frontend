namespace FrontendSchemeRegistration.Application.Services;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Extensions;
using Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Options;

[ExcludeFromCodeCoverage]
public class PaymentCalculationServiceApiClient : IPaymentCalculationServiceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string[] _scopes;
    private readonly ITokenAcquisition _tokenAcquisition;

    public PaymentCalculationServiceApiClient(HttpClient httpClient, ITokenAcquisition tokenAcquisition, IOptions<PaymentFacadeApiOptions> options)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _scopes = new[] { options.Value.DownstreamScope };
    }

    public async Task<HttpResponseMessage> SendPostRequest<T>(string endpoint, T body)
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        _httpClient.AddHeaderAcceptJson();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Bearer, accessToken);

        var response = await _httpClient.PostAsJsonAsync(endpoint, body);
        response.EnsureSuccessStatusCode();

        return response;
    }
}