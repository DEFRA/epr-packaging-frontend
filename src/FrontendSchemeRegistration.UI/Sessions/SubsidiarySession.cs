using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.Sessions;
public class SubsidiarySession
{
    public Company Company { get; set; }

    public bool? IsUserChangingDetails { get; set; }

    public List<string> Journey { get; set; } = new();

    public Nation? UkNation { get; set; }
}
