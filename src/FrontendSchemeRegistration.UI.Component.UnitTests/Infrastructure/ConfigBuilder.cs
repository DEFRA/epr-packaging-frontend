namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

[ExcludeFromCodeCoverage]
public static class ConfigBuilder
{
    public static IConfigurationRoot GenerateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var data = new Dictionary<string, string?>
        {
            ["IsStubAuth"] = "true",
            ["UseLocalSession"] = "true",
            ["WebAPI:BaseEndpoint"] = "http://localhost:9091",
            ["PaymentFacadeApi:BaseUrl"] = "http://localhost:9091",
            ["EprAuthorizationConfig:FacadeBaseUrl"] = "http://localhost:9091/api/",
            ["AccountsFacadeAPI:BaseEndpoint"] = "http://localhost:9091/api/",
            ["StartupUtcTimestampOverride"] = "2026-03-27T08:58:00Z",
            ["Csoc:WasteObligationsBaseAddress"] = "https://understanding-obligations"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                data[key] = value;
            }
        }

        var configSource = new MemoryConfigurationSource { InitialData = data };
        var provider = new MemoryConfigurationProvider(configSource);

        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}