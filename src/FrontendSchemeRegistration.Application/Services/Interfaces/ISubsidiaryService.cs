namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.Subsidiary;

public interface ISubsidiaryService
{
    Task<string> SaveSubsidiary(SubsidiaryDto subsidiary);

    Task<string> AddSubsidiary(SubsidiaryAddDto subsidiary);
}