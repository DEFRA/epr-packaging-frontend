namespace FrontendSchemeRegistration.UI.IntegrationTests;

using System.Net.Http;
using Controllers.FrontendSchemeRegistration;
using FrontendSchemeRegistration.MockServer.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

public abstract class TestBase
{
    private WireMockServer _mockApiServer = null!;
    private WebApplicationFactory<FrontendSchemeRegistrationController> _factory = null!;
    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public async Task InitializeAsync()
    {
        _mockApiServer = WireMockServer.Start();
        _mockApiServer.WithWebApi();
        _mockApiServer.WithAccounts();

        _factory = new IntegrationTestFactory(_mockApiServer.Url!);

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        await AuthenticateAsync();
    }

    [TearDown]
    public void Cleanup()
    {
        Client.Dispose();
        _factory.Dispose();
        _mockApiServer.Stop();
        _mockApiServer.Dispose();
    }

    /// <summary>
    /// Registers a WireMock stub that returns <paramref name="prnData"/> for GET /api/v1/prn/{externalId}.
    /// Call this in each test before making requests that need a specific PRN.
    /// </summary>
    protected void SetupPrnById(string externalId, object prnData)
    {
        _mockApiServer
            .Given(Request.Create().UsingGet().WithPath($"/api/v1/prn/{externalId}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(prnData));
    }

    /// <summary>
    /// Registers a WireMock stub that returns <paramref name="prns"/> for GET /api/v1/prn/organisation.
    /// Used by the CSV download and list endpoints.
    /// </summary>
    protected void SetupPrnOrganisationList(IEnumerable<object> prns)
    {
        _mockApiServer
            .Given(Request.Create().UsingGet().WithPath("/api/v1/prn/organisation"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(prns.ToArray()));
    }

    private async Task AuthenticateAsync()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", "test@test.com" },
            { "UserId", "9e4da0ed-cdff-44a1-8ae0-cef7f22b914b" },
            { "ReturnUrl", "/home" }
        });
        await Client.PostAsync("/services/account-details", formData);
    }

    private sealed class IntegrationTestFactory(string mockServerUrl)
        : WebApplicationFactory<FrontendSchemeRegistrationController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("ComponentTest");

            var testConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IsStubAuth"] = "true",
                    ["UseLocalSession"] = "true",
                    ["BasePath"] = "/report-data",
                    ["FeatureManagement:ShowPrn"] = "true",
                    ["FeatureManagement:EnableCsvDownload"] = "true",
                    ["WebAPI:BaseEndpoint"] = mockServerUrl,
                    ["PaymentFacadeApi:BaseUrl"] = mockServerUrl,
                    ["EprAuthorizationConfig:FacadeBaseUrl"] = $"{mockServerUrl}/api/",
                    ["AccountsFacadeAPI:BaseEndpoint"] = $"{mockServerUrl}/api/",
                    ["StartupUtcTimestampOverride"] = "2026-03-27T08:58:00Z"
                })
                .Build();

            builder.UseConfiguration(testConfig);
        }
    }
}
