namespace FrontendSchemeRegistration.Application.ClassMaps;

using CsvHelper.Configuration;
using DTOs.Subsidiary;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ExportOrganisationSubsidiariesRowMap : ClassMap<ExportOrganisationSubsidiariesResponseModel>
{
    public ExportOrganisationSubsidiariesRowMap()
    {
        Map(m => m.OrganisationId).Name("organisation_id");
        Map(m => m.SubsidiaryId).Name("subsidiary_id");
        Map(m => m.OrganisationName).Name("organisation_name");
        Map(m => m.CompaniesHouseNumber).Name("companies_house_number");
    }
}