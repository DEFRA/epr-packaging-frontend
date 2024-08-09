using FrontendSchemeRegistration.Application.DTOs.Subsidiary;

namespace FrontendSchemeRegistration.Application.Services.Interfaces;

public interface ISubsidiaryService
{
    Task<string> SaveSubsidiary(SubsidiaryDto subsidiary);

    Task<string> AddSubsidiary(SubsidiaryAddDto subsidiary);

    Task<Stream> GetSubsidiariesStreamAsync(int subsidiaryParentId, bool isComplienceScheme);
}