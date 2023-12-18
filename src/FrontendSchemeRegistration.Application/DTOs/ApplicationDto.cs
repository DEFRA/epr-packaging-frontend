using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ApplicationDto
{
    public Guid Id { get; set; }

    public Guid CustomerOrganisationId { get; set; }

    public Guid CustomerId { get; set; }

    public List<UserDto> Users { get; set; } = new ();
}