using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ISubsidiaryService
{
    Task<string> SaveSubsidiary(SubsidiaryDto subsidiary);

    Task<string> AddSubsidiary(SubsidiaryAddDto subsidiary);

    Task<Stream> GetSubsidiariesStreamAsync(Guid organisationId, Guid? complianceSchemeId, bool isComplianceScheme, bool includeSubsidiaryJoinerAndLeaverColumns);

    Task<PaginatedResponse<RelationshipResponseModel>> GetPagedOrganisationSubsidiaries(int page, int showPerPage);

    Task<OrganisationRelationshipModel> GetOrganisationSubsidiaries(Guid organisationId);

    Task<SubsidiaryFileUploadStatus> GetSubsidiaryFileUploadStatusAsync(Guid userId, Guid organisationId);
    Task SetSubsidiaryFileUploadStatusViewedAsync(bool value, Guid userId, Guid organisationId);
    Task<bool> GetSubsidiaryFileUploadStatusViewedAsync(Guid userId, Guid organisationId);

    Task<OrganisationDto> GetOrganisationByReferenceNumber(string referenceNumber);

    Task TerminateSubsidiary(Guid parentOrganisationExternalId, Guid childOrganisationId, Guid userId);

    Task<SubsidiaryUploadStatusDto> GetUploadStatus(Guid userId, Guid organisationId);

    Task<Stream> GetUploadErrorsReport(Guid userId, Guid organisationId);

    Task<Stream?> GetAllSubsidiariesStream();
}