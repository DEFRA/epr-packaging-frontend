using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.Application.Services
{
    public class RegulatorService : IRegulatorService
    {
        private const string SendResubmissionEmailErrorMessage = "Attempting to send Resubmission Email to Regulator failed";

        private readonly ILogger<RegulatorService> _logger;
        private readonly IAccountServiceApiClient _accountServiceApiClient;

        public RegulatorService(ILogger<RegulatorService> logger, IAccountServiceApiClient accountServiceApiClient)
        {
            _logger = logger;
            _accountServiceApiClient = accountServiceApiClient;
        }

        public async Task<string> SendRegulatorResubmissionEmail(ResubmissionEmailRequestModel input)
        {
            try
            {
                var result = await _accountServiceApiClient.SendPostRequest(RegulatorPaths.ResubmissionEmail, input);
                var content = await result.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<string>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, SendResubmissionEmailErrorMessage);
                throw;
            }
        }
    }
}
