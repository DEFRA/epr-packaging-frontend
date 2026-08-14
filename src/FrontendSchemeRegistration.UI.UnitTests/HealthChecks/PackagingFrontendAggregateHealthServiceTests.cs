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

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Status.Should().Be("Healthy");
        handler.RequestUris.Should().BeEquivalentTo(
            [
                "https://gateway.test/admin/health",
                "https://account.test/admin/health",
                "https://payment.test/admin/health",
            ]);
        handler.ClientNames.Should().BeEquivalentTo(
            [
                DownstreamHealthClientNames.WebApiGateway,
                DownstreamHealthClientNames.AccountsFacade,
                DownstreamHealthClientNames.PaymentFacade,
            ]);
        var gatewayResult = report.Results["WebApiGateway"];
        gatewayResult.Endpoint.Should().Be("https://gateway.test/admin/health");
        gatewayResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        gatewayResult.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        gatewayResult.Response.Should().BeNull();
    }

    [Test]
    public async Task CheckAsync_WhenDeep_CallsTheGatewayExtendedEndpointOnly()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler);

        var report = await service.CheckAsync(true, 0, CancellationToken.None);

        handler.RequestUris.Should().Contain("https://gateway.test/admin/health/all?deep=true");
        handler.RequestUris.Should().Contain("https://account.test/admin/health");
        handler.RequestUris.Should().Contain("https://payment.test/admin/health");
        handler.Requests.Single(request => request.Uri.EndsWith("/health/all?deep=true", StringComparison.Ordinal)).Hop.Should().Be("1");
        report.Results["WebApiGateway"].Response!.ToJsonString().Should().Be("{\"status\":\"Healthy\",\"results\":{}}");
    }

    [Test]
    public async Task CheckAsync_WhenDeep_AddsTheNextHopOnlyToTheGateway()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler);

        await service.CheckAsync(true, 1, CancellationToken.None);

        handler.Requests.Single(request => request.Uri.EndsWith("/health/all?deep=true", StringComparison.Ordinal)).Hop.Should().Be("2");
        handler.Requests.Where(request => !request.Uri.EndsWith("/health/all?deep=true", StringComparison.Ordinal)).Should().AllSatisfy(request => request.Hop.Should().BeNull());
    }

    [Test]
    public async Task CheckAsync_WhenMaximumHopReached_UsesTheGatewayShallowHealth()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler);

        var report = await service.CheckAsync(true, 2, CancellationToken.None);

        report.DeepLimited.Should().BeTrue();
        handler.RequestUris.Should().Contain("https://gateway.test/admin/health");
        handler.RequestUris.Should().NotContain("https://gateway.test/admin/health/all?deep=true");
        handler.Requests.Single(request => request.Uri == "https://gateway.test/admin/health").Hop.Should().BeNull();
        report.Results["WebApiGateway"].Response.Should().BeNull();
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    public async Task CheckAsync_WhenDownstreamRejectsTheRequest_ReportsAnAuthenticationFailure(HttpStatusCode statusCode)
    {
        var handler = new RecordingHandler((_, _) => CreateResponse(statusCode));
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Status.Should().Be("Unhealthy");
        report.Results.Values.Should().AllSatisfy(result =>
        {
            result.Status.Should().Be("Unhealthy");
            result.Failure.Should().Be("authentication");
        });
    }

    [Test]
    public async Task CheckAsync_WhenTheGatewayReturnsAnUnsuccessfulStatus_ReportsItAsUnhealthy()
    {
        var handler = new RecordingHandler((request, _) => CreateResponse(
            request.RequestUri!.AbsolutePath.EndsWith("/health", StringComparison.Ordinal)
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK));
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Status.Should().Be("Unhealthy");
        report.Results["WebApiGateway"].Status.Should().Be("Unhealthy");
        report.Results["WebApiGateway"].Failure.Should().BeNull();
    }

    [Test]
    public async Task CheckAsync_WhenTheDeepResponseIsNotJson_ReportsAnInvalidResponse()
    {
        var handler = new RecordingHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                request.RequestUri!.AbsolutePath.EndsWith("/health/all", StringComparison.Ordinal) ? "not json" : "Healthy",
                Encoding.UTF8,
                "application/json"),
        }));
        var service = CreateService(handler);

        var report = await service.CheckAsync(true, 0, CancellationToken.None);

        report.Status.Should().Be("Unhealthy");
        report.Results["WebApiGateway"].Failure.Should().Be("invalid_response");
    }

    [Test]
    public async Task CheckAsync_WhenTheDeepResponseExceedsTheMaximumSize_ReportsAnInvalidResponse()
    {
        var handler = new RecordingHandler((_, _) => CreateResponse(HttpStatusCode.OK, "{\"status\":\"Healthy\"}"));
        var service = CreateService(handler, healthAllOptions: new HealthAllOptions
        {
            Token = "test-token",
            MaximumResponseBodyBytes = 1,
        });

        var report = await service.CheckAsync(true, 0, CancellationToken.None);

        report.Status.Should().Be("Unhealthy");
        report.Results["WebApiGateway"].Failure.Should().Be("invalid_response");
    }

    [Test]
    public async Task CheckAsync_WhenTheDeepResponseExceedsTheMaximumSizeWithoutAContentLength_ReportsAnInvalidResponse()
    {
        var handler = new RecordingHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = request.RequestUri!.AbsolutePath.EndsWith("/health/all", StringComparison.Ordinal)
                ? new UnknownLengthContent("{\"status\":\"Healthy\"}")
                : new StringContent("Healthy", Encoding.UTF8, "application/json"),
        }));
        var service = CreateService(handler, healthAllOptions: new HealthAllOptions
        {
            Token = "test-token",
            MaximumResponseBodyBytes = 1,
        });

        var report = await service.CheckAsync(true, 0, CancellationToken.None);

        report.Status.Should().Be("Unhealthy");
        report.Results["WebApiGateway"].Failure.Should().Be("invalid_response");
    }

    [Test]
    public async Task CheckAsync_WhenTheDownstreamIsUnavailable_ReportsItAsUnavailable()
    {
        var handler = new RecordingHandler((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException()));
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Results.Values.Should().AllSatisfy(result => result.Failure.Should().Be("unavailable"));
    }

    [Test]
    public async Task CheckAsync_WhenTheDownstreamTimesOut_ReportsItAsTimedOut()
    {
        var handler = new RecordingHandler((_, _) => Task.FromException<HttpResponseMessage>(new OperationCanceledException()));
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Results.Values.Should().AllSatisfy(result => result.Failure.Should().Be("timeout"));
    }

    [Test]
    public async Task CheckAsync_WhenTheRequestIsCancelled_PropagatesTheCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var handler = new RecordingHandler((_, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        var service = CreateService(handler);

        var action = () => service.CheckAsync(false, 0, cancellationTokenSource.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task CheckAsync_WhenTheEndpointIsInvalid_ReportsAConfigurationFailure()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler, webApiOptions: new WebApiOptions { BaseEndpoint = "not-a-url" });

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Results["WebApiGateway"].Failure.Should().Be("configuration");
    }

    [Test]
    public async Task CheckAsync_WhenBuildingTheEndpointFails_ReportsAConfigurationFailure()
    {
        var handler = new RecordingHandler();
        var service = CreateService(handler, webApiOptions: new WebApiOptions { BaseEndpoint = null! });

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Results["WebApiGateway"].Failure.Should().Be("configuration");
    }

    [Test]
    public async Task CheckAsync_WhenAnUnexpectedErrorOccurs_ReportsTheDownstreamAsUnavailable()
    {
        var handler = new RecordingHandler((_, _) => Task.FromException<HttpResponseMessage>(new InvalidOperationException()));
        var service = CreateService(handler);

        var report = await service.CheckAsync(false, 0, CancellationToken.None);

        report.Results.Values.Should().AllSatisfy(result => result.Failure.Should().Be("unavailable"));
    }

    private static PackagingFrontendAggregateHealthService CreateService(
        RecordingHandler handler,
        HealthAllOptions? healthAllOptions = null,
        WebApiOptions? webApiOptions = null) => new(
        new TestHttpClientFactory(handler),
        Options.Create(webApiOptions ?? new WebApiOptions { BaseEndpoint = "https://gateway.test/gateway" }),
        Options.Create(new AccountsFacadeApiOptions { BaseEndpoint = "https://account.test/account/api/" }),
        Options.Create(new PaymentFacadeApiOptions { BaseUrl = "https://payment.test/payment/api/v1/" }),
        Options.Create(healthAllOptions ?? new HealthAllOptions { Token = "test-token" }));

    private static Task<HttpResponseMessage> CreateResponse(HttpStatusCode statusCode, string body = "Healthy") =>
        Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    private sealed class TestHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            handler.ClientNames.Add(name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseFactory = null) : HttpMessageHandler
    {
        public ConcurrentBag<string> RequestUris { get; } = [];

        public ConcurrentBag<string> ClientNames { get; } = [];

        public ConcurrentBag<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            Requests.Add(new RecordedRequest(
                request.RequestUri.ToString(),
                request.Headers.TryGetValues(AggregateHealthHop.HeaderName, out var values) ? values.Single() : null));

            if (responseFactory is not null)
            {
                return responseFactory(request, cancellationToken);
            }

            var body = request.RequestUri.AbsolutePath.EndsWith("/health/all", StringComparison.Ordinal)
                ? "{\"status\":\"Healthy\",\"results\":{}}"
                : "Healthy";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        public sealed record RecordedRequest(string Uri, string? Hop);
    }

    private sealed class UnknownLengthContent(string content) : HttpContent
    {
        private readonly byte[] _content = Encoding.UTF8.GetBytes(content);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_content).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
