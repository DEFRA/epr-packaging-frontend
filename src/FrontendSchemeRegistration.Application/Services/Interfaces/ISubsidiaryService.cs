using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ISubsidiaryService
{
    Task<string> SaveSubsidiary(SubsidiaryDto subsidiary);

    Task<string> AddSubsidiary(SubsidiaryAddDto subsidiary);

    Task<Stream> GetSubsidiariesStreamAsync(Guid organisationId, Guid? complianceSchemeId, bool isComplianceScheme);

    Task<OrganisationRelationshipModel> GetOrganisationSubsidiaries(Guid organisationId);
}