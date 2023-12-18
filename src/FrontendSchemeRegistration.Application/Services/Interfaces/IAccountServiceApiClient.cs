namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IAccountServiceApiClient
{
    Task<HttpResponseMessage> SendGetRequest(string endpoint);

    Task<HttpResponseMessage> SendGetRequest(Guid organisationId, string endpoint);

    Task<HttpResponseMessage> SendPostRequest<T>(string endpoint, T body);

    Task<HttpResponseMessage> PutAsJsonAsync<T>(Guid organisationId, string endpoint, T body);

    void AddHttpClientHeader(string key, string value);

    void RemoveHttpClientHeader(string key);
}