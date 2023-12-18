namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.ComplianceScheme;

public interface IComplianceSchemeService
{
    Task ClearSummaryCache(Guid organisationId, Guid complianceSchemeId);

    bool HasCache();

    Task<ProducerComplianceSchemeDto?> GetProducerComplianceScheme(Guid producerOrganisationId);

    Task<List<ComplianceSchemeDto>> GetOperatorComplianceSchemes(Guid operatorOrganisationId);

    Task<IEnumerable<ComplianceSchemeDto>?> GetComplianceSchemes();

    Task<ComplianceSchemeSummary> GetComplianceSchemeSummary(Guid organisationId, Guid complianceSchemeId);

    Task<SelectedSchemeDto> ConfirmAddComplianceScheme(Guid complianceSchemeId, Guid organisationId);

    Task<SelectedSchemeDto> ConfirmUpdateComplianceScheme(Guid complianceSchemeId, Guid selectedSchemeId, Guid producerOrganisationId);

    Task<HttpResponseMessage> StopComplianceScheme(Guid selectedSchemeId, Guid organisationId);
}