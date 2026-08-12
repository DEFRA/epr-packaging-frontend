namespace FrontendSchemeRegistration.UI.HealthChecks;

using System.Diagnostics;
using System.Net;
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

    public async Task<AggregateHealthReport> CheckAsync(bool deep, int hop, CancellationToken cancellationToken)
    {
        var effectiveDeep = deep && hop < healthAllOptions.Value.MaximumDeepHealthHops;
        var checks = new[]
        {
            CheckAsync("WebApiGateway", DownstreamHealthClientNames.WebApiGateway, () => WebApiGatewayHealth(webApiOptions.Value.BaseEndpoint, effectiveDeep), effectiveDeep, effectiveDeep ? hop : null, cancellationToken),
            CheckAsync("AccountsFacade", DownstreamHealthClientNames.AccountsFacade, () => AdminHealth(accountsFacadeApiOptions.Value.BaseEndpoint), false, null, cancellationToken),
            CheckAsync("PaymentFacade", DownstreamHealthClientNames.PaymentFacade, () => AdminHealth(paymentFacadeApiOptions.Value.BaseUrl), false, null, cancellationToken),
        };

        var results = await Task.WhenAll(checks);
        var resultMap = results.ToDictionary(result => result.Name, result => result.Result, StringComparer.Ordinal);
        var status = resultMap.Values.All(result => result.Status == Healthy) ? Healthy : Unhealthy;

        return new AggregateHealthReport(status, resultMap, deep && !effectiveDeep);
    }

    private async Task<(string Name, DownstreamHealthResult Result)> CheckAsync(
        string name,
        string clientName,
        Func<Uri> endpointFactory,
        bool includeResponse,
        int? hop,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, healthAllOptions.Value.DownstreamTimeoutSeconds)));
        Uri? endpoint = null;

        try
        {
            endpoint = endpointFactory();
            var response = await SendHealthRequestAsync(clientName, endpoint, includeResponse, hop, timeout.Token);

            return (name, new DownstreamHealthResult(
                response.IsHealthy ? Healthy : Unhealthy,
                SafeEndpoint(endpoint),
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                response.Body,
                response.Failure));
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

    private async Task<HealthCheckResponse> SendHealthRequestAsync(
        string clientName,
        Uri endpoint,
        bool includeResponse,
        int? hop,
        CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (hop.HasValue)
        {
            AggregateHealthHop.AddTo(request, hop.Value);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = includeResponse ? await ReadJsonResponseAsync(response, cancellationToken) : null;
        var failure = DetermineFailure(response, includeResponse, body);

        return new HealthCheckResponse(
            response.IsSuccessStatusCode && failure is null,
            (int)response.StatusCode,
            body,
            failure);
    }

    private static string? DetermineFailure(HttpResponseMessage response, bool includeResponse, JsonNode? body)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "authentication";
        }

        if (includeResponse && body is null)
        {
            return "invalid_response";
        }

        return null;
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
        new(new Uri(baseUrl, UriKind.Absolute), deep ? "/admin/health/all?deep=true" : "/admin/health");

    private static Uri AdminHealth(string baseUrl) => new(new Uri(baseUrl, UriKind.Absolute), "/admin/health");

    private static string SafeEndpoint(Uri endpoint)
    {
        var builder = new UriBuilder(endpoint) { UserName = string.Empty, Password = string.Empty, Query = string.Empty };
        return builder.Uri.ToString();
    }

    private sealed record HealthCheckResponse(bool IsHealthy, int StatusCode, JsonNode? Body, string? Failure);
}

public sealed record AggregateHealthReport(
    string Status,
    IReadOnlyDictionary<string, DownstreamHealthResult> Results,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool DeepLimited = false);

public sealed record DownstreamHealthResult(
    string Status,
    string Endpoint,
    int? StatusCode,
    long DurationMs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonNode? Response = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Failure = null);
