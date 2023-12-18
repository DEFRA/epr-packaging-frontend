using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class DelegatedPersonNominatorDto
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string OrganisationName { get; set; }
}