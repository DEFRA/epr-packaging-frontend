namespace FrontendSchemeRegistration.UI.Controllers;

using Application.DTOs.ComplianceScheme;
using EPR.Common.Authorization.Models;
using Sessions;

public static class SetupMinimalSession
{
    public static FrontendSchemeRegistrationSession FrontendSchemeRegistrationSession(
        List<ComplianceSchemeDto> complianceSchemes,
        UserData userData, Guid? selectedComplianceSchemeId)
    {
        var session = NewSession(userData);
        var defaultComplianceScheme = complianceSchemes.FirstOrDefault();
        if (selectedComplianceSchemeId.HasValue)
        {
            defaultComplianceScheme = complianceSchemes.FirstOrDefault(x => x.Id == selectedComplianceSchemeId);
        }
       
        session.RegistrationSession.SelectedComplianceScheme ??= defaultComplianceScheme;
        return session;
    }

    public static FrontendSchemeRegistrationSession FrontendSchemeRegistrationSession(ProducerComplianceSchemeDto? producerComplianceSchemeDto,
        UserData userData)
    {
        var session = NewSession(userData);
        session.RegistrationSession.CurrentComplianceScheme ??= producerComplianceSchemeDto;
        return session;
    }
    
    private static FrontendSchemeRegistrationSession NewSession(UserData userData)
    {
        var session = new FrontendSchemeRegistrationSession();
        session.UserData = userData;
        return session;
    }
     
}