namespace FrontendSchemeRegistration.Application.Services;

using FrontendSchemeRegistration.Application.DTOs.RegistrationSubmission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class RegistrationSubmissionDataService : IRegistrationSubmissionDataService
{
    private readonly IPaymentCalculationServiceApiClient _apiClient;
    private readonly ILogger<RegistrationSubmissionDataService> _logger;
    private readonly PaymentFacadeApiOptions _options;

    public RegistrationSubmissionDataService(
        IPaymentCalculationServiceApiClient apiClient,
        IOptions<PaymentFacadeApiOptions> options,
        ILogger<RegistrationSubmissionDataService> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(CreateRegistrationSubmissionDataRequest request)
    {
        if (request is null)
        {
            return;
        }

        try
        {
            await _apiClient.SendPostRequest(_options.Endpoints.RegistrationSubmissionDataEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify payment facade of registration submission data for SubmissionId {SubmissionId} FileId {FileId}",
                request.SubmissionId,
                request.FileId);
        }
    }
}
