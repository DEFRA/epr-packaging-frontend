namespace FrontendSchemeRegistration.UI.UnitTests.HealthChecks;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UI.HealthChecks;

[TestFixture]
public class HealthHttpClientServiceCollectionExtensionsTests
{
    [Test]
    public void AddAggregateHealthHttpClients_RegistersEachNamedClient()
    {
        using var serviceProvider = new ServiceCollection()
            .AddAggregateHealthHttpClients()
            .BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        foreach (var clientName in new[]
                 {
                     DownstreamHealthClientNames.WebApiGateway,
                     DownstreamHealthClientNames.AccountsFacade,
                     DownstreamHealthClientNames.PaymentFacade,
                 })
        {
            using var client = clientFactory.CreateClient(clientName);

            client.Should().NotBeNull();
        }
    }
}
