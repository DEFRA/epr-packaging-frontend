using System.ComponentModel.DataAnnotations;

namespace FrontendSchemeRegistration.UI.Sessions;

using System.ComponentModel.DataAnnotations;

public class SchemeMembershipSession
{
    public List<string> Journey { get; set; } = new();

    public string SelectedReasonForRemoval { get; set; }

    [MaxLength(200)]
    public string? TellUsMore { get; set; }

    public string? RemovedSchemeMember { get; set; }
}
