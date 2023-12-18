namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}