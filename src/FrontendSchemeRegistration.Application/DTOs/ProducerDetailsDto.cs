using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ProducerDetailsDto
{
    public int OrganisationId { get; set; }

    public int NumberOfSubsidiaries { get; set; }

    public int NumberOfSubsidiariesBeingOnlineMarketPlace { get; set; }

    public string ProducerSize { get; set; } = string.Empty;

    public bool IsOnlineMarketplace { get; set; }
}