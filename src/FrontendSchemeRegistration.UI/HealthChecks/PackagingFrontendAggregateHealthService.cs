namespace FrontendSchemeRegistration.UI.HealthChecks;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Application.Options;
using Microsoft.Extensions.Options;

public sealed class PackagingFrontendAggregateHealthService(
    IHttpClientFactory httpClientFactory,
    IOptions<WebApiOptions> webApiOptions,
    IOptions<AccountsFacadeApiOptions> accountsFacadeApiOptions,
    IOptions<PaymentFacadeApiOptions> paymentFacadeApiOptions,
    IOptions<HealthAllOptions> healthAllOptions)
{
    private const string Healthy = "Healthy";
    private const string Unhealthy = "Unhealthy";

    public async Task<AggregateHealthReport> CheckAsync(bool deep, CancellationToken cancellationToken)
    {
        var checks = new[]
        {
            CheckAsync("WebApiGateway", () => WebApiGatewayHealth(webApiOptions.Value.BaseEndpoint, deep), deep, cancellationToken),
            CheckAsync("AccountsFacade", () => AdminHealth(accountsFacadeApiOptions.Value.BaseEndpoint), false, cancellationToken),
            CheckAsync("PaymentFacade", () => AdminHealth(paymentFacadeApiOptions.Value.BaseUrl), false, cancellationToken),
        };

        var results = await Task.WhenAll(checks);
        var resultMap = results.ToDictionary(result => result.Name, result => result.Result, StringComparer.Ordinal);
        var status = resultMap.Values.All(result => result.Status == Healthy) ? Healthy : Unhealthy;

        return new AggregateHealthReport(status, resultMap);
    }

    private async Task<(string Name, DownstreamHealthResult Result)> CheckAsync(
        string name,
        Func<Uri> endpointFactory,
        bool includeResponse,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, healthAllOptions.Value.DownstreamTimeoutSeconds)));
        Uri? endpoint = null;

        try
        {
            endpoint = endpointFactory();
            using var client = httpClientFactory.CreateClient();
            using var response = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            var body = includeResponse ? await ReadJsonResponseAsync(response, timeout.Token) : null;
            var failure = includeResponse && body is null ? "invalid_response" : null;
            var isHealthy = response.IsSuccessStatusCode && failure is null;

            return (name, new DownstreamHealthResult(
                isHealthy ? Healthy : Unhealthy,
                SafeEndpoint(endpoint),
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                body,
                failure));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (name, new DownstreamHealthResult(Unhealthy, SafeEndpoint(endpoint!), null, stopwatch.ElapsedMilliseconds, Failure: "timeout"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return (name, new DownstreamHealthResult(Unhealthy, SafeEndpoint(endpoint!), null, stopwatch.ElapsedMilliseconds, Failure: "unavailable"));
        }
        catch (UriFormatException)
        {
            return (name, new DownstreamHealthResult(Unhealthy, "not configured", null, stopwatch.ElapsedMilliseconds, Failure: "configuration"));
        }
        catch (Exception)
        {
            return (name, new DownstreamHealthResult(Unhealthy, endpoint is null ? "not configured" : SafeEndpoint(endpoint), null, stopwatch.ElapsedMilliseconds, Failure: endpoint is null ? "configuration" : "unavailable"));
        }
    }

    private async Task<JsonNode?> ReadJsonResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var maxBytes = Math.Max(1, healthAllOptions.Value.MaximumResponseBodyBytes);
        if (response.Content.Headers.ContentLength is long contentLength && contentLength > maxBytes)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var readBuffer = new byte[Math.Min(81920, maxBytes + 1)];
        while (true)
        {
            var bytesToRead = (int)Math.Min(readBuffer.Length, maxBytes - buffer.Length + 1);
            var bytesRead = await stream.ReadAsync(readBuffer.AsMemory(0, bytesToRead), cancellationToken);
            if (bytesRead == 0)
                break;

            await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
            if (buffer.Length > maxBytes)
                return null;
        }

        try
        {
            return JsonNode.Parse(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Uri WebApiGatewayHealth(string baseUrl, bool deep) =>
        new(EnsureTrailingSlash(baseUrl), deep ? "admin/health/all?deep=true" : "admin/health");

    private static Uri AdminHealth(string baseUrl) => new(EnsureTrailingSlash(baseUrl), "../admin/health");

    private static Uri EnsureTrailingSlash(string baseUrl) => new(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);

    private static string SafeEndpoint(Uri endpoint)
    {
        var builder = new UriBuilder(endpoint) { UserName = string.Empty, Password = string.Empty, Query = string.Empty };
        return builder.Uri.ToString();
    }
}

public sealed record AggregateHealthReport(string Status, IReadOnlyDictionary<string, DownstreamHealthResult> Results);

public sealed record DownstreamHealthResult(
    string Status,
    string Endpoint,
    int? StatusCode,
    long DurationMs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonNode? Response = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Failure = null);
