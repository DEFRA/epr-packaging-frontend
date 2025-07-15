using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.Application.DTOs.UserAccount;

[ExcludeFromCodeCoverage]
public class Organisation
{
    public Guid Id { get; set; }

    [JsonProperty("name")]
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public string OrganisationRole { get; set; }

    public string OrganisationType { get; set; }
}