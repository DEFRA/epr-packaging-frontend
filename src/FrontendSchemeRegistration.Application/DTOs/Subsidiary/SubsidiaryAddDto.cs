using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

[ExcludeFromCodeCoverage]
public class SubsidiaryAddDto
{
    public Guid? ParentOrganisationId { get; init; }

    public Guid? ChildOrganisationId { get; init; }
}
