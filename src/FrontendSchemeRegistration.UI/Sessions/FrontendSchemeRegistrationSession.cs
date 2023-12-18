namespace FrontendSchemeRegistration.UI.Sessions;

using EPR.Common.Authorization.Interfaces;
using EPR.Common.Authorization.Models;

public class FrontendSchemeRegistrationSession : IHasUserData
{
    public UserData UserData { get; set; } = new();

    public RegistrationSession RegistrationSession { get; set; } = new();

    public NominatedDelegatedPersonSession NominatedDelegatedPersonSession { get; set; } = new();

    public SchemeMembershipSession SchemeMembershipSession { get; set; } = new();
}
