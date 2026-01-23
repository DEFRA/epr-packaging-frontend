namespace FrontendSchemeRegistration.UI.ComponentTests.Infrastructure;

using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Net.Http.Headers;

public interface ITestHttpClient : IDisposable
{
    Task<HttpResponseMessage> GetAsync(string url);
    Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> content);
}

public class ComponentTestClient(TestServer server, string baseUrl = "https://localhost")
    : ITestHttpClient
{
    private CookieContainer CookieContainer { get; } = new();
    private TestServer Server { get; } = server;
    private Uri SubstituteBaseUrl { get; } = new(baseUrl);

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var response = await BuildRequest(url).GetAsync();

        return response;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> content)
    {
        var getResponse = await GetAsync(url);
        
        var html = await getResponse.Content.ReadAsStringAsync();

        //TODO - investigate why this isnt correctly validating, at the moment the verification is disabled for component tests
        if (!content.ContainsKey("__RequestVerificationToken"))
        {
            var token = ExtractRequestVerificationToken(html);
            content.Add("__RequestVerificationToken", token);
        }
        
        var response = await BuildRequest(url)
            .And(req => req.Content = new FormUrlEncodedContent(content)).PostAsync();
        
        UpdateCookies(url, response);
        return response;
    }
    
    public void Dispose()
    {
    }
    
    private RequestBuilder BuildRequest(string url)
    {
        var uri = new Uri(SubstituteBaseUrl, url);
        var builder = Server.CreateRequest(url);
        builder.TestServer.BaseAddress = new Uri("https://localhost");
        
        var cookieHeader = CookieContainer.GetCookieHeader(uri);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            builder.AddHeader(HeaderNames.Cookie, cookieHeader);
        }

        return builder;
    }

    private void UpdateCookies(string url, HttpResponseMessage response)
    {
        if (response.Headers.Contains(HeaderNames.SetCookie))
        {
            var uri = new Uri(SubstituteBaseUrl, url);
            var cookies = response.Headers.GetValues(HeaderNames.SetCookie);
            foreach (var cookie in cookies)
            {
                CookieContainer.SetCookies(uri, cookie);
            }
        }
    }

    private static string ExtractRequestVerificationToken(string html)
    {
        const string tokenFieldName = "\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(tokenFieldName, StringComparison.CurrentCultureIgnoreCase) +
                         tokenFieldName.Length;
        var endIndex = html.IndexOf("\"", startIndex, StringComparison.CurrentCultureIgnoreCase);
        return html.Substring(startIndex, endIndex - startIndex);
    }
}