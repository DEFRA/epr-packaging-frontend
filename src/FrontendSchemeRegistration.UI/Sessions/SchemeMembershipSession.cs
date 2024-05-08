namespace FrontendSchemeRegistration.UI.Sessions;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SchemeMembershipSession
{
    public List<string> Journey { get; set; } = new();

    public string SelectedReasonForRemoval { get; set; }

    [MaxLength(200)]
    public string? TellUsMore { get; set; }

    public string? RemovedSchemeMember { get; set; }
}
