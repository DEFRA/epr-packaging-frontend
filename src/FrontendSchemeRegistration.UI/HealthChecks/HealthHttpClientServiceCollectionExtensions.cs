namespace FrontendSchemeRegistration.UI.HealthChecks;

public static class HealthHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddAggregateHealthHttpClients(this IServiceCollection services)
    {
        AddHealthClient(services, DownstreamHealthClientNames.WebApiGateway);
        AddHealthClient(services, DownstreamHealthClientNames.AccountsFacade);
        AddHealthClient(services, DownstreamHealthClientNames.PaymentFacade);

        return services;
    }

    private static void AddHealthClient(IServiceCollection services, string clientName)
    {
        services
            .AddHttpClient(clientName);
    }
}
