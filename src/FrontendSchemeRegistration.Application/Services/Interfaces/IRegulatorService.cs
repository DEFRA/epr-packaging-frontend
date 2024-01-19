using FrontendSchemeRegistration.Application.RequestModels;

namespace FrontendSchemeRegistration.Application.Services.Interfaces
{
    public interface IRegulatorService
    {
        Task<string> SendRegulatorResubmissionEmail(ResubmissionEmailRequestModel input);
    }
}