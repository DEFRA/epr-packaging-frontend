namespace FrontendSchemeRegistration.UI.Component.UnitTests.Steps;

using System.Net;
using System.Text.RegularExpressions;
using Extensions;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class HttpSteps(ScenarioContext context)
{
    [Given("I have navigated to the (.*)")]
    [When("I navigate to the (.*)")]
    public async Task WhenINavigateToThePage(string pageName)
    {
        var page = context.GetPage(pageName);
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(page.Url);

        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
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
    
    [When("I continue to the Registration Task List")]
    public async Task WhenIContinueToTheRegistrationTaskList()
    {
        var pageContent = context.Get<string>(ContextKeys.HttpResponseContent);
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);

        var fileUploadLinkMatch = Regex.Match(pageContent, @"href\s*=\s*[""']([^""']*redirect-upload-organisation-details[^""']*)[""']", RegexOptions.IgnoreCase);
        if (fileUploadLinkMatch.Success)
        {
            return;
        }

        var taskListLinkMatch = Regex.Match(pageContent, @"href\s*=\s*[""']([^""']*registration-task-list[^""']*)[""']", RegexOptions.IgnoreCase);
        taskListLinkMatch.Success.Should().BeTrue("page should contain a link to the Registration Task List or already be on it (has file upload link)");
        var pathAndQuery = taskListLinkMatch.Groups[1].Value;
        if (pathAndQuery.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            pathAndQuery = new Uri(pathAndQuery).PathAndQuery;

        var response = await client.GetAsync(pathAndQuery);
        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I start the file upload step")]
    public async Task WhenIStartTheFileUploadStep()
    {
        var pageContent = context.Get<string>(ContextKeys.HttpResponseContent);
        var hrefMatch = Regex.Match(pageContent, @"href\s*=\s*[""']([^""']*redirect-upload-organisation-details[^""']*)[""']", RegexOptions.IgnoreCase);
        hrefMatch.Success.Should().BeTrue("page should contain a link to start the file upload step");
        var pathAndQuery = hrefMatch.Groups[1].Value;
        if (pathAndQuery.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            pathAndQuery = new Uri(pathAndQuery).PathAndQuery;

        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.GetAsync(pathAndQuery);

        while (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
        {
            var redirectUrl = response.Headers.Location.ToString();
            var redirectPath = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            response = await client.GetAsync(redirectPath);
        }

        var uploadPagePath = response.RequestMessage?.RequestUri?.PathAndQuery ?? "/report-data/upload-organisation-details";
        context.Set(uploadPagePath, ContextKeys.UploadPageUrl);
        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I select a submission period and start the POM file upload")]
    public async Task WhenISelectASubmissionPeriodAndStartThePomFileUpload()
    {
        var pageContent = context.TryGetValue<string>(ContextKeys.HttpResponseContent, out var storedContent)
            ? storedContent
            : string.Empty;

        var periodMatch = Regex.Match(pageContent, @"name=""dataPeriod""\s+value=""([^""]+)""", RegexOptions.IgnoreCase);
        var dataPeriod = periodMatch.Success ? periodMatch.Groups[1].Value : "January to June 2025";

        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        var response = await client.PostAsync("/report-data/file-upload-sub-landing", new Dictionary<string, string>
        {
            { "dataPeriod", dataPeriod }
        });

        while (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
        {
            var redirectUrl = response.Headers.Location.ToString();
            var redirectPath = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            response = await client.GetAsync(redirectPath);
        }

        var uploadPagePath = response.RequestMessage?.RequestUri?.PathAndQuery ?? "/report-data/file-upload";
        context.Set(uploadPagePath, ContextKeys.UploadPageUrl);
        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I upload a valid POM CSV file")]
    public async Task WhenIUploadAValidPomCsvFile()
    {
        var uploadPageUrl = context.TryGetValue<string>(ContextKeys.UploadPageUrl, out var storedUrl)
            ? storedUrl!
            : "/report-data/file-upload";
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);

        var csvContent = "organisation_id,subsidiary_id,quantity_kg,packaging_type,packaging_category,material_type,material_subtype,from_home_nation,to_home_nation\n" +
                         "123456,,100,PB,PF,AL,,EN,EN\n";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

        var response = await client.PostWithFileAsync(uploadPageUrl, fileBytes, "packaging-data.csv");

        while (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
        {
            var redirectUrl = response.Headers.Location.ToString();
            var pathAndQuery = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            response = await client.GetAsync(pathAndQuery);
        }

        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I upload a valid CSV file")]
    public async Task WhenIUploadAValidCsvFile()
    {
        var uploadPageUrl = context.TryGetValue<string>(ContextKeys.UploadPageUrl, out var storedUrl)
            ? storedUrl!
            : context.GetPage("Company Details Upload Page").Url;
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);

        var csvContent = "organisation_id,organisation_name\n123456,Test Org Ltd\n";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var formData = new Dictionary<string, string> { { "registrationyear", "2026" } };

        var response = await client.PostWithFileAsync(uploadPageUrl, fileBytes, "organisation-details.csv", formData);

        while (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
        {
            var redirectUrl = response.Headers.Location.ToString();
            var pathAndQuery = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            response = await client.GetAsync(pathAndQuery);
        }

        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I confirm and submit the packaging data")]
    public async Task WhenIConfirmAndSubmitThePackagingData()
    {
        var pageContent = context.Get<string>(ContextKeys.HttpResponseContent);
        var response = context.Get<HttpResponseMessage>(ContextKeys.HttpResponse);
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);

        var submitUrl = response.RequestMessage?.RequestUri?.PathAndQuery
            ?? "/report-data/file-upload-check-file-and-submit";

        // Locate the hidden <input> for LastValidFileId and extract its value,
        // regardless of attribute order (id/type may appear before value).
        var inputTagMatch = Regex.Match(pageContent, @"<input[^>]+name=""LastValidFileId""[^>]*/?>", RegexOptions.IgnoreCase);
        var valueMatch = inputTagMatch.Success
            ? Regex.Match(inputTagMatch.Value, @"value=""([^""]+)""", RegexOptions.IgnoreCase)
            : Match.Empty;
        var lastValidFileId = valueMatch.Success ? valueMatch.Groups[1].Value : string.Empty;

        var currentResponse = await client.PostAsync(submitUrl, new Dictionary<string, string>
        {
            { "LastValidFileId", lastValidFileId }
        });

        while (currentResponse.StatusCode == HttpStatusCode.Redirect && currentResponse.Headers.Location != null)
        {
            var redirectUrl = currentResponse.Headers.Location.ToString();
            var redirectPath = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            currentResponse = await client.GetAsync(redirectPath);
        }

        context.Set(currentResponse, ContextKeys.HttpResponse);
        context.Set(await currentResponse.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [When("I complete the resubmission fee journey")]
    public async Task WhenINavigateToTheResubmissionFeePage()
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);

        // Initialise the session by loading the sub-landing page, which populates
        // PackagingResubmissionApplicationSessions from the packaging-resubmission API.
        var subLandingResponse = await client.GetAsync("/report-data/file-upload-sub-landing");
        var subLandingContent  = await subLandingResponse.Content.ReadAsStringAsync();

        // Extract the dataPeriod for the InProgress submission period from the page form.
        var periodMatch = Regex.Match(subLandingContent, @"name=""dataPeriod""\s+value=""([^""]+)""", RegexOptions.IgnoreCase);
        var dataPeriod  = periodMatch.Success ? periodMatch.Groups[1].Value : "January to June 2025";

        // POST the selected period; controller validates acceptance history and redirects to
        // the resubmission task list when IsResubmissionInProgress = true.
        var postResponse = await client.PostAsync("/report-data/file-upload-sub-landing", new Dictionary<string, string>
        {
            { "dataPeriod", dataPeriod }
        });

        var response = postResponse;
        while (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
        {
            var redirectUrl  = response.Headers.Location.ToString();
            var redirectPath = redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(redirectUrl).PathAndQuery
                : redirectUrl;
            response = await client.GetAsync(redirectPath);
        }

        // Navigate to the fee calculations page (session is now fully set up from the task-list load).
        response = await client.GetAsync("/report-data/resubmission-fee-calculations");

        context.Set(response, ContextKeys.HttpResponse);
        context.Set(await response.Content.ReadAsStringAsync(), ContextKeys.HttpResponseContent);
    }

    [Then("I am on the (.*)")]
    public void ThenIAmOnThePage(string pageName)
    {
        var page = context.GetPage(pageName);
        var response = context.Get<HttpResponseMessage>(ContextKeys.HttpResponse);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "expected to land on {0}", pageName);
        var requestUrl = response.RequestMessage?.RequestUri?.ToString() ?? "";
        requestUrl.Should().Contain(page.Url, "expected to be on page {0}", pageName);
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