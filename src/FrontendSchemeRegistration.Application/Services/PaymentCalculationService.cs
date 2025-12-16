using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using FrontendSchemeRegistration.Application.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;

#pragma warning disable CA2254

namespace FrontendSchemeRegistration.Application.Services;

public class PaymentCalculationService(
    IAccountServiceApiClient accountServiceApiClient,
    IPaymentCalculationServiceApiClient paymentCalculationServiceApiClient,
    ILogger<PaymentCalculationService> logger,
    IOptions<PaymentFacadeApiOptions> options)
    : IPaymentCalculationService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<PaymentCalculationResponse?> GetProducerRegistrationFees(PaymentCalculationRequest request) =>
        PostToPaymentCalculationService<PaymentCalculationRequest, PaymentCalculationResponse>(
            options.Value.Endpoints.ProducerRegistrationFeesEndpoint,
            request,
            (l, refNum) => l.LogError("registration fees details Not Found fees for producer reference {ReferenceNumber}", refNum),
            (l, ex, refNum) => l.LogError(ex, "Failed to retrieve registration fees for producer reference {ReferenceNumber}", refNum),
            request.ApplicationReferenceNumber);

    public Task<PaymentCalculationResponse?> GetProducerRegistrationFees(ProducerPaymentCalculationV2Request request) =>
        PostToPaymentCalculationService<ProducerPaymentCalculationV2Request, PaymentCalculationResponse>(
            options.Value.Endpoints.ProducerRegistrationFeesEndpoint,
            request,
            (l, refNum) => l.LogError("V2 Service registration fees details Not Found fees for producer reference {ReferenceNumber}", refNum),
            (l, ex, refNum) => l.LogError(ex, "V2 Service Failed to retrieve registration fees for producer reference {ReferenceNumber}", refNum),
            request.ApplicationReferenceNumber);

    public Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationRequest request) =>
        PostToPaymentCalculationService<ComplianceSchemePaymentCalculationRequest, ComplianceSchemePaymentCalculationResponse>(
            options.Value.Endpoints.ComplianceSchemeRegistrationFeesEndpoint,
            request,
            (l, refNum) => l.LogError("registration fees details Not Found for compliance scheme reference {ReferenceNumber}", refNum),
            (l, ex, refNum) => l.LogError(ex, "Failed to retrieve registration fees for compliance scheme reference {ReferenceNumber}", refNum),
            request.ApplicationReferenceNumber);

    public Task<ComplianceSchemePaymentCalculationResponse?> GetComplianceSchemeRegistrationFees(ComplianceSchemePaymentCalculationV2Request request) =>
        PostToPaymentCalculationService<ComplianceSchemePaymentCalculationV2Request, ComplianceSchemePaymentCalculationResponse>(
            options.Value.Endpoints.ComplianceSchemeRegistrationFeesEndpoint,
            request,
            (l, refNum) => l.LogError("V2 Service registration fees details Not Found for compliance scheme reference {ReferenceNumber}", refNum),
            (l, ex, refNum) => l.LogError(ex, "V2 Service Failed to retrieve registration fees for compliance scheme reference {ReferenceNumber}", refNum),
            request.ApplicationReferenceNumber);

    public async Task<string> InitiatePayment(PaymentInitiationRequest request)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.OnlinePaymentsEndpoint, request);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Redirect URL not found in the initialise Payment response");
                return string.Empty;
            }

            var htmlContent = await result.Content.ReadAsStringAsync();

            const string pattern = @"window\.location\.href\s*=\s*'(?<url>.*?)';";

            var match = Regex.Match(htmlContent, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }

            logger.LogWarning("Redirect URL not found in the initialise Payment response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate payment for {OrganisationId}", request.OrganisationId);
        }

        return string.Empty;
    }

    public async Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate)
    {
        var endpoint = isComplianceScheme ? options.Value.Endpoints.ComplianceSchemeResubmissionFeesEndpoint : options.Value.Endpoints.ProducerResubmissionFeesEndpoint;
        var callType = isComplianceScheme ? "compliance scheme" : "producer";

        try
        {
            if (isComplianceScheme && memberCount == 0)
            {
                logger.LogInformation("Member count is 0 for this compliance scheme");
                return new PackagingPaymentResponse();
            }

            var request = new PackagingPaymentRequest
            {
                ReferenceNumber = applicationReferenceNumber,
                Regulator = regulatorNation,
                MemberCount = memberCount,
                ResubmissionDate = resubmissionDate
            };

            var result = await paymentCalculationServiceApiClient.SendPostRequest(endpoint, request);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null!;
            }

            result.EnsureSuccessStatusCode();
            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            var feeResponse = JsonSerializer.Deserialize<PackagingPaymentResponse>(jsonContent, _options);

            return feeResponse!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve resubmission fees for {CallType} and reference - {ApplicationReferenceNumber}", callType, applicationReferenceNumber);
        }

        return null!;
    }

    public async Task<string> GetRegulatorNation(Guid? organisationId)
    {
        try
        {
            var result = await accountServiceApiClient.SendGetRequest($"organisations/regulator-nation?organisationId={organisationId}");
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return string.Empty;
            }

            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<string>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve regulator's nation for {OrganisationId}", organisationId);
            return string.Empty;
        }
    }

    private static string RemoveDecimalValues(string jsonString)
    {
        return Regex.Replace(jsonString, @"(\d+)\.0+", "$1", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }

    private async Task<TResponse?> PostToPaymentCalculationService<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Action<ILogger, string> logNotFound,
        Action<ILogger, Exception, string> logError,
        string referenceNumber)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(endpoint, request);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                logNotFound(logger, referenceNumber);
                return default;
            }

            result.EnsureSuccessStatusCode();

            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            return JsonSerializer.Deserialize<TResponse>(jsonContent, _options);
        }
        catch (Exception ex)
        {
            logError(logger, ex, referenceNumber);
            return default;
        }
    }
}