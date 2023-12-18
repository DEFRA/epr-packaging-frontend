namespace FrontendSchemeRegistration.UI.Sessions;

public class NominatedDelegatedPersonSession
{
    public List<string> Journey { get; set; } = new();

    public string TelephoneNumber { get; set; }

    public string NominatorFullName { get; set; }

    public string NomineeFullName { get; set; }

    public string OrganisationName { get; set; }
}