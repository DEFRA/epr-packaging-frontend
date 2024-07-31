using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

[ExcludeFromCodeCoverage]
public class SubsidiaryDto
{
    public OrganisationModel Subsidiary { get; init; }

    public Guid? ParentOrganisationId { get; init; }

    public Guid UserId { get; init; }
}
