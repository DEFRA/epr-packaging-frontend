using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using FrontendSchemeRegistration.Application.Options;

namespace FrontendSchemeRegistration.Application.Services;

public class PaymentCalculationService(
    IPaymentCalculationServiceApiClient paymentCalculationServiceApiClient,
    ILogger<PaymentCalculationService> logger,
    IOptions<PaymentFacadeApiOptions> options)
    : IPaymentCalculationService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<PaymentCalculationResponse?> GetProducerRegistrationFees(PaymentCalculationRequest request)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.ProducerRegistrationFeesEndpoint, request);
            
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogError("registration fees details Not Found fees for producer reference {ReferenceNumber}", request.ApplicationReferenceNumber);
                return null;
            }

            result.EnsureSuccessStatusCode();
            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            var feeResponse =  JsonSerializer.Deserialize<PaymentCalculationResponse>(jsonContent, _options);

			return feeResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve registration fees for producer reference {ReferenceNumber}", request.ApplicationReferenceNumber);
        }

		return null;
    }

    public async Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationRequest request)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.ComplianceSchemeRegistrationFeesEndpoint, request);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogError("registration fees details Not Found for compliance scheme reference {ReferenceNumber}", request.ApplicationReferenceNumber);
                return null;
            }

            result.EnsureSuccessStatusCode();

            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            var feeResponse = JsonSerializer.Deserialize<ComplianceSchemePaymentCalculationResponse>(jsonContent, _options);

            return feeResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve registration fees for compliance scheme reference {ReferenceNumber}", request.ApplicationReferenceNumber);
        }

        return null;
    }

    public async Task<string> InitiatePayment(PaymentInitiationRequest request)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.OnlinePaymentsEndpoint, request);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Redirect URL not found in the initialise Payment response.");
                return string.Empty;
            }
            
            var htmlContent = await result.Content.ReadAsStringAsync();

            const string pattern = @"window\.location\.href\s*=\s*'(?<url>.*?)';";

            var match = Regex.Match(htmlContent, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }

            logger.LogWarning("Redirect URL not found in the initialise Payment response.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate payment for {OrganisationId}", request.OrganisationId);
        }
        return string.Empty;
    }

    private static string RemoveDecimalValues(string jsonString)
    {
        return Regex.Replace(jsonString, @"(\d+)\.0+", "$1", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }
}
