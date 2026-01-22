namespace FrontendSchemeRegistration.Application.ConfigurationExtensions;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Options;
using Polly;
using Services;
using Services.Interfaces;

[ExcludeFromCodeCoverage]
public static class HttpClient
{
    public static void AddWebApiGatewayClient(this IServiceCollection services)
    {
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HttpClientOptions>>().Value;

        services
            .AddHttpClient<IWebApiGatewayClient, WebApiGatewayClient>()
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
                options.RetryCount, _ => TimeSpan.FromSeconds(options.RetryDelaySeconds)));
    }
}