namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Hosted service that warms up <see cref="IRegistrationPeriodProvider"/> at application startup by
/// fetching the submission-period rows from the payment facade and loading them into the singleton
/// provider. Ensures the provider's synchronous public API returns populated data on the first
/// request served after startup completes.
/// </summary>
public class RegistrationPeriodProviderWarmupService(
    IServiceScopeFactory scopeFactory,
    IRegistrationPeriodProvider registrationPeriodProvider,
    ILogger<RegistrationPeriodProviderWarmupService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var paymentCalculationService = scope.ServiceProvider.GetRequiredService<IPaymentCalculationService>();

            var periods = await paymentCalculationService.GetSubmissionPeriods();
            if (periods is null || periods.Length == 0)
            {
                logger.LogError("No submission periods returned by the payment facade during startup warmup");
                return;
            }

            registrationPeriodProvider.Load(periods);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to hydrate registration windows at startup from the payment facade");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
