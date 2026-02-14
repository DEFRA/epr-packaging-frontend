namespace FrontendSchemeRegistration.UI.ComponentTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Net.Http.Headers;

[ExcludeFromCodeCoverage]
public class EnvironmentTestClient(string baseUrl) : TestClientBase
{
    private CookieContainer CookieContainer { get; } = new();
    private readonly HttpClient _httpClient = new();

    public override async Task<HttpResponseMessage> GetAsync(string url)
    {
        var request = BuildRequest(url, HttpMethod.Get);
        
        var response = await _httpClient.SendAsync(request);
        //UpdateCookies(url, response);
        return response;
    }

    public override async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> content)
    {
        var getResponse = await GetAsync(url);
        
        var html = await getResponse.Content.ReadAsStringAsync();
        if (!content.ContainsKey("__RequestVerificationToken"))
        {
            var token = ExtractRequestVerificationToken(html);
            content.Add("__RequestVerificationToken", token);
        }
        
        var response = await _httpClient.SendAsync(BuildRequest(url, HttpMethod.Post, new FormUrlEncodedContent(content)));
        //UpdateCookies(url, response);
        return response;
    }

    private HttpRequestMessage BuildRequest(string url, HttpMethod method, HttpContent? content = null)
    {
        var request = new HttpRequestMessage();
        var uri = new Uri(new Uri(baseUrl), url);
        request.RequestUri = uri;

        var cookieHeader = CookieContainer.GetCookieHeader(uri);

        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.Add(HeaderNames.Cookie, cookieHeader);
        }
        request.Method = method;
        if (content != null)
        {
            request.Content = content;
        }

        return request;
    }

    private void UpdateCookies(string url, HttpResponseMessage response)
    {
        if (response.Headers.Contains(HeaderNames.SetCookie))
        {
            var uri = new Uri(new Uri(baseUrl), url);
            var cookies = response.Headers.GetValues(HeaderNames.SetCookie);
            foreach (var cookie in cookies)
            {
                CookieContainer.SetCookies(uri, cookie);
            }
        }
    }
    
    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}