namespace FrontendSchemeRegistration.UI.UnitTests.HealthChecks;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Application.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UI.HealthChecks;

[TestFixture]
public class PackagingFrontendAggregateHealthServiceTests
{
    [Test]
    public async Task CheckAsync_WhenNotDeep_CallsShallowHealthEndpoints()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, CancellationToken.None);

        report.Status.Should().Be("Healthy");
        handler.RequestUris.Should().BeEquivalentTo(
            [
                "https://gateway.test/gateway/admin/health",
                "https://account.test/account/admin/health",
                "https://payment.test/payment/admin/health",
            ]);
        handler.ClientNames.Should().BeEquivalentTo(
            [
                DownstreamHealthClientNames.WebApiGateway,
                DownstreamHealthClientNames.AccountsFacade,
                DownstreamHealthClientNames.PaymentFacade,
            ]);
        report.Results["WebApiGateway"].Response.Should().BeNull();
    }

    [Test]
    public async Task CheckAsync_WhenDeep_CallsTheGatewayExtendedEndpointOnly()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler);

        var report = await service.CheckAsync(true, CancellationToken.None);

        handler.RequestUris.Should().Contain("https://gateway.test/gateway/admin/health/all?deep=true");
        handler.RequestUris.Should().Contain("https://account.test/account/admin/health");
        handler.RequestUris.Should().Contain("https://payment.test/payment/admin/health");
        report.Results["WebApiGateway"].Response!.ToJsonString().Should().Be("{\"status\":\"Healthy\",\"results\":{}}");
    }

    private static PackagingFrontendAggregateHealthService CreateService(RecordingHandler handler) => new(
        new TestHttpClientFactory(handler),
        Options.Create(new WebApiOptions { BaseEndpoint = "https://gateway.test/gateway" }),
        Options.Create(new AccountsFacadeApiOptions { BaseEndpoint = "https://account.test/account/api/" }),
        Options.Create(new PaymentFacadeApiOptions { BaseUrl = "https://payment.test/payment/api/" }),
        Options.Create(new HealthAllOptions { Token = "test-token" }));

    private sealed class TestHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            handler.ClientNames.Add(name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public ConcurrentBag<string> RequestUris { get; } = [];

        public ConcurrentBag<string> ClientNames { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            var body = request.RequestUri.AbsolutePath.EndsWith("/health/all", StringComparison.Ordinal)
                ? "{\"status\":\"Healthy\",\"results\":{}}"
                : "Healthy";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
