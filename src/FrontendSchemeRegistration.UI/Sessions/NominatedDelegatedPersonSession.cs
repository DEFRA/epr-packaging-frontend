using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class NominatedDelegatedPersonSession
{
    public List<string> Journey { get; set; } = new();

    public string TelephoneNumber { get; set; }

    public string NominatorFullName { get; set; }

    public string NomineeFullName { get; set; }

    public string OrganisationName { get; set; }
}