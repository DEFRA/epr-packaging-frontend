using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class SynapseResponse
{
    public string OrganisationId { get; set; } = string.Empty;

    public bool IsFileSynced { get; set; }
}