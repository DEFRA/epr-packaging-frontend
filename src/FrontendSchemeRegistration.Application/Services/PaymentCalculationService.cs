using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using FrontendSchemeRegistration.Application.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;

namespace FrontendSchemeRegistration.Application.Services;

public class PaymentCalculationService(
	IAccountServiceApiClient accountServiceApiClient,
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

    public async Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate)
	{
		var endpoint = isComplianceScheme ? options.Value.Endpoints.ComplianceSchemeResubmissionFeesEndpoint : options.Value.Endpoints.ProducerResubmissionFeesEndpoint;
		var callType = isComplianceScheme ? "compliance scheme" : "producer";

		try
		{
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
			logger.LogError(ex, "Failed to retrieve resubmission fees for {callType} and reference - {applicationReferenceNumber}", callType, applicationReferenceNumber);
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
}
