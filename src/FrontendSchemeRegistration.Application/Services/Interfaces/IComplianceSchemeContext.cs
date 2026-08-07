namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IComplianceSchemeContext
{
    Task<Guid?> GetComplianceSchemeIdAsync();
}
