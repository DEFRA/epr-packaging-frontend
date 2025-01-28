using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace FrontendSchemeRegistration.Application.Services;

using FrontendSchemeRegistration.Application.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

public class PaymentCalculationService(
    IAccountServiceApiClient accountServiceApiClient,
    IWebApiGatewayClient webApiGatewayClient,
    IPaymentCalculationServiceApiClient paymentCalculationServiceApiClient,
    ILogger<PaymentCalculationService> logger,
    IOptions<PaymentFacadeApiOptions> options)
    : IPaymentCalculationService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<PaymentCalculationResponse> GetProducerRegistrationFees(
                 ProducerDetailsDto producerDetails, string applicationReferenceNumber, bool isLateFeeApplicable, Guid? organisationId, DateTime registrationSubmissionDate)
    {
        try
        {
            var regulatorNation = await GetRegulatorNation(organisationId);

            var request = new PaymentCalculationRequest
            {
                Regulator = regulatorNation,
                ApplicationReferenceNumber = applicationReferenceNumber,
                IsLateFeeApplicable = isLateFeeApplicable,
                IsProducerOnlineMarketplace = producerDetails.IsOnlineMarketplace,
                NoOfSubsidiariesOnlineMarketplace = producerDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
                NumberOfSubsidiaries = producerDetails.NumberOfSubsidiaries,
                ProducerType = producerDetails.ProducerSize,
                SubmissionDate = registrationSubmissionDate
            };

            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.ProducerRegistrationFeesEndpoint, request);
            
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null!;
            }

            result.EnsureSuccessStatusCode();
            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            var feeResponse =  JsonSerializer.Deserialize<PaymentCalculationResponse>(jsonContent, _options);

			return feeResponse!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve registration fees for producer reference {ReferenceNumber}", applicationReferenceNumber);
        }

		return null!;
    }

    public async Task<ComplianceSchemePaymentCalculationResponse> GetComplianceSchemeRegistrationFees(ComplianceSchemeDetailsDto complianceSchemeDetails, string applicationReferenceNumber, Guid? organisationId)
    {
        try
        {
            var regulatorNation = await GetRegulatorNation(organisationId);

            var request = new ComplianceSchemePaymentCalculationRequest
            {
                Regulator = regulatorNation,
                ApplicationReferenceNumber = applicationReferenceNumber,
                SubmissionDate = DateTime.UtcNow,
                ComplianceSchemeMembers = complianceSchemeDetails.Members.Select(_ => new ComplianceSchemePaymentCalculationRequestMember
                {
                    IsLateFeeApplicable = _.IsLateFeeApplicable,
                    IsOnlineMarketplace = _.IsOnlineMarketplace,
                    MemberId = _.MemberId,
                    MemberType = _.MemberType,
                    NoOfSubsidiariesOnlineMarketplace = _.NumberOfSubsidiariesBeingOnlineMarketPlace,
                    NumberOfSubsidiaries = _.NumberOfSubsidiaries
                }).ToList()
            };

            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.ComplianceSchemeRegistrationFeesEndpoint, request);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null!;
            }

            result.EnsureSuccessStatusCode();

            var jsonContent = RemoveDecimalValues(await result.Content.ReadAsStringAsync());
            var feeResponse = JsonSerializer.Deserialize<ComplianceSchemePaymentCalculationResponse>(jsonContent, _options);

            return feeResponse!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve registration fees for compliance scheme reference {ReferenceNumber}", applicationReferenceNumber);
        }

        return null!;
    }

    public async Task<ComplianceSchemeDetailsDto> GetComplianceSchemeDetails(string organisationId)
    {
        return await webApiGatewayClient.GetComplianceSchemeDetails(organisationId);
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

    public async Task<string> InitiatePayment(PaymentInitiationRequest request)
    {
        try
        {
            var result = await paymentCalculationServiceApiClient.SendPostRequest(options.Value.Endpoints.OnlinePaymentsEndpoint, request);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return string.Empty;
            }
            
            var htmlContent = await result.Content.ReadAsStringAsync();

            const string pattern = @"window\.location\.href\s*=\s*'(?<url>.*?)';";

            var match = Regex.Match(htmlContent, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }
            else
            {
                logger.LogWarning("Redirect URL not found in the initialise Payment response.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate payment for {OrganisationId}", request.OrganisationId);
        }
        return string.Empty;
    }

    public string CreateApplicationReferenceNumber(bool isComplianceScheme, int csRowNumber, string organisationNumber, SubmissionPeriod period)
    {
        var referenceNumber = organisationNumber;
        var periodEnd = DateTime.Parse($"30 {period.EndMonth} {period.Year}", new CultureInfo("en-GB"));
        var periodNumber = DateTime.Today <= periodEnd ? 1 : 2;

        if (isComplianceScheme)
        {
            referenceNumber += csRowNumber.ToString("D3");
        }

        return $"PEPR{referenceNumber}{(periodEnd.Year - 2000)}P{periodNumber}";
    }

    private static string RemoveDecimalValues(string jsonString)
    {
        return Regex.Replace(jsonString, @"(\d+)\.0+", "$1", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }
}
