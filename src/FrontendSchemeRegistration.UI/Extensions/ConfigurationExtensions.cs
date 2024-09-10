using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Extensions
{
    public static class ConfigurationExtensions
    {
        public static bool IsFeatureEnabled(this IConfiguration configuration, string feature)
        {
            var featureServices = new ServiceCollection();
            featureServices.AddFeatureManagement(configuration);
            using var provider = featureServices.BuildServiceProvider();
            var manager = provider.GetRequiredService<IFeatureManager>();

            return manager.IsEnabledAsync(feature).GetAwaiter().GetResult();
        }
    }
}
