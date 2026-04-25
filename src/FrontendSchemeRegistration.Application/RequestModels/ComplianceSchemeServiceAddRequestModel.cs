using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels;

public class ComplianceSchemeServiceAddRequestModel
{
    public Guid OrganisationId { get; set; }

    public Guid ComplianceSchemeId { get; set; }
}