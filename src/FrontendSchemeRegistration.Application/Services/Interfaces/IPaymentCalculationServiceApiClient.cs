namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IPaymentCalculationServiceApiClient
{
    Task<HttpResponseMessage> SendPostRequest<T>(string endpoint, T body);
}