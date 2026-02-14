namespace FrontendSchemeRegistration.UI.ComponentTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

[ExcludeFromCodeCoverage]
public static class ConfigBuilder
{
    public static IConfigurationRoot GenerateConfiguration()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData =
            [
                new KeyValuePair<string,string>("IsStubAuth", "true"),
                new KeyValuePair<string,string>("UseLocalSession", "true"),
                new KeyValuePair<string,string>("WebAPI:BaseEndpoint", "http://localhost:9091"),
                new KeyValuePair<string,string>("PaymentFacadeApi:BaseUrl", "http://localhost:9091"),
                new KeyValuePair<string,string>("EprAuthorizationConfig:FacadeBaseUrl", "http://localhost:9091/api/"),
                new KeyValuePair<string,string>("AccountsFacadeAPI:BaseEndpoint", "http://localhost:9091/api/")
            ]
        };
        var provider = new MemoryConfigurationProvider(configSource);
        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}