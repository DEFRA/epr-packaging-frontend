namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ICloner
{
    T Clone<T>(T source);
}