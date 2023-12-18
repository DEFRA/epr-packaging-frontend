using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeServiceUpdateRequestModel
{
    public Guid SelectedSchemeId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid ComplianceSchemeId { get; set; }
}