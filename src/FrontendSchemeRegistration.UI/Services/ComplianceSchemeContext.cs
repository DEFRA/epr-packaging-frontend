using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;

namespace FrontendSchemeRegistration.UI.Services;

public class ComplianceSchemeContext(
    IComplianceSchemeMemberService complianceSchemeMemberService,
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IHttpContextAccessor httpContextAccessor) : IComplianceSchemeContext
{
    public async Task<Guid?> GetComplianceSchemeIdAsync()
    {
        var complianceSchemeId = complianceSchemeMemberService.GetComplianceSchemeId();
        if (complianceSchemeId.HasValue)
        {
            return complianceSchemeId;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var session = await sessionManager.GetSessionAsync(httpContext.Session);
        return session?.RegistrationSession.SelectedComplianceScheme?.Id;
    }
}
