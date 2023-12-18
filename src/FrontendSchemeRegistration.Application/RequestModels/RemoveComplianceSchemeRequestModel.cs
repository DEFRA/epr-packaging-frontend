using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels;

[ExcludeFromCodeCoverage]
public class RemoveComplianceSchemeRequestModel
{
    public Guid SelectedSchemeId { get; set; }

    public Guid OrganisationId { get; set; }
}