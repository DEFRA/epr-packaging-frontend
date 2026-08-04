namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Background service that hydrates <see cref="IRegistrationPeriodProvider"/> from the payment facade
/// at application startup. Retries with backoff on failure or empty response so a transient upstream
/// outage (cold payment-service, network blip) does not leave the provider empty for the process lifetime.
/// The loop exits as soon as the provider reports itself loaded.
/// </summary>
public class RegistrationPeriodProviderWarmupService(
    IServiceScopeFactory scopeFactory,
    IRegistrationPeriodProvider registrationPeriodProvider,
    ILogger<RegistrationPeriodProviderWarmupService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await TryLoadAsync())
            {
                return;
            }

            var delay = RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)];
            attempt++;
            logger.LogInformation("Retrying submission-period warmup in {Delay}.", delay);

            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<bool> TryLoadAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var paymentCalculationService = scope.ServiceProvider.GetRequiredService<IPaymentCalculationService>();

            var periods = await paymentCalculationService.GetSubmissionPeriods();
            registrationPeriodProvider.Load(periods);
            logger.LogInformation("Loaded {Count} submission periods from the payment facade.", periods.Length);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to hydrate registration windows from the payment facade.");
            return false;
        }
    }
}
