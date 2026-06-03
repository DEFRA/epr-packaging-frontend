namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using FrontendSchemeRegistration.Application.DTOs.RegistrationSubmission;

public interface IRegistrationSubmissionDataService
{
    Task NotifyAsync(CreateRegistrationSubmissionDataRequest request);
}
