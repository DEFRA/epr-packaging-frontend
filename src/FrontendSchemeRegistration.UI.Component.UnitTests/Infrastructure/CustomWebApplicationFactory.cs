namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[ExcludeFromCodeCoverage]
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public Action<IServiceCollection> ConfigureTestServices { get; set; } = _ => { };
    public new Dictionary<string, string?> AdditionalConfig { get; set; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder
                .AddConfiguration(ConfigBuilder.GenerateConfiguration())
                .AddInMemoryCollection(AdditionalConfig));
        
        builder.ConfigureTestServices(ConfigureTestServices);
    }
}