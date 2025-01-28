using Azure;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;

namespace FrontendSchemeRegistration.UI.Services
{
    public class SubsidiaryUtilityService : ISubsidiaryUtilityService
    {
        private readonly ISubsidiaryService _subsidiaryService;
        private readonly IComplianceSchemeMemberService _complianceSchemeMemberService;

        public SubsidiaryUtilityService(ISubsidiaryService subsidiaryService, IComplianceSchemeMemberService complianceSchemeMemberService)
        {
            _subsidiaryService = subsidiaryService;
            _complianceSchemeMemberService = complianceSchemeMemberService;
        }
        public async Task<int> GetSubsidiariesCount(string organisationRole, Guid organisationId, Guid? selectedSchemeId)
        {
            if (organisationRole == OrganisationRoles.Producer)
            {
                var response = await _subsidiaryService.GetOrganisationSubsidiaries(organisationId);
                return response?.Relationships?.Count ?? 0;
            }

            if (!selectedSchemeId.HasValue)
            {
                throw new ArgumentException("Selected scheme ID must be provided for compliance schemes.", nameof(selectedSchemeId));
            }

            var complianceSchemeMembershipResponse = await _complianceSchemeMemberService.GetComplianceSchemeMembers(
                organisationId, selectedSchemeId.Value, pageSize: 1, searchQuery: string.Empty, page: 1, hideNoSubsidiaries: true);

            return complianceSchemeMembershipResponse?.SubsidiariesCount ?? 0;
        }
    }
}
