using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface IComplianceSchemeMemberService
{
    Task<ComplianceSchemeMembershipResponse> GetComplianceSchemeMembers(
        Guid organisationId, Guid complianceSchemeId, int pageSize, string searchQuery, int page, bool hideNoSubsidiaries);

    Task<ComplianceSchemeMemberDetails?> GetComplianceSchemeMemberDetails(Guid organisationId, Guid selectedSchemeId);

    Task<IReadOnlyCollection<ComplianceSchemeReasonsRemovalDto>?> GetReasonsForRemoval();

    Task<RemovedComplianceSchemeMember> RemoveComplianceSchemeMember(Guid organisationId, Guid complianceSchemeId, Guid selectedSchemeId, string reasonCode, string? tellUsMore);

    Guid? GetComplianceSchemeId();
}