namespace FrontendSchemeRegistration.UI.ComponentTests.Steps;

using System.Net;
using Extensions;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class HttpSteps(ScenarioContext context)
{
    [When("I navigate to the (.*)")]
    public async Task WhenINavigateToThePage(string pageName)
    {
        var page = context.GetPage(pageName);
        
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(page.Url);
        context.Set(response,ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(),ContextKeys.HttpResponseContent);
    }

    [When("I browse to the following url: (.*)")]
    public async Task WhenINavigateToTheFollowingUrl(string url)
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(url);
        context.Set(response,ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(),ContextKeys.HttpResponseContent);
    }

    [Then("I am redirected to the: (.*)")]
    public async Task ThenIamRedirectedToThePage(string pageName)
    {
        var page = context.GetPage(pageName);
        var redirection = context.Get<HttpResponseMessage>(ContextKeys.HttpResponse);
        redirection.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var redirectUrl = redirection.Headers.Location.ToString();
        redirectUrl.Should().Contain(page.Url);
        
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(redirectUrl);
        var responseContent = await response.Content.ReadAsStringAsync();

        context.Remove(ContextKeys.HttpResponseRedirectContent);
        context.Set(responseContent,ContextKeys.HttpResponseRedirectContent);
    }
    
    [Then("I am redirected to the url: (.*)")]
    public async Task ThenIamRedirectedToTheFollowingUrl(string url)
    {
        var redirection = context.Get<HttpResponseMessage>(ContextKeys.HttpResponse);
        redirection.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var redirectUrl = redirection.Headers.Location.ToString();
        redirectUrl.Should().Be(url);
        
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(redirectUrl);
        var responseContent = await response.Content.ReadAsStringAsync();

        context.Remove(ContextKeys.HttpResponseRedirectContent);
        context.Set(responseContent,ContextKeys.HttpResponseRedirectContent);
    }
    
    [Then("the page is (.*)")]
    public async Task ThenThePageIsReturned(string httpStatusCode)
    {
        var page = context.GetStatusCode(httpStatusCode);

        if (!context.TryGetValue<HttpResponseMessage>(ContextKeys.HttpResponse, out var httpResponse))
        {
            Assert.Fail($"Scenario context does not contain value for key {ContextKeys.HttpResponse}");
        }

        httpResponse.StatusCode.Should().Be((HttpStatusCode)page.StatusCode);
    }
}