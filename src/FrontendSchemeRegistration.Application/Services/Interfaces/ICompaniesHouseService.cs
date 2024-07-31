namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.CompaniesHouse;

public interface ICompaniesHouseService
{
    Task<Company?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber);
}