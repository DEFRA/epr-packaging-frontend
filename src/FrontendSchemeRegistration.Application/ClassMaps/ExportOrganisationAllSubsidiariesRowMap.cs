namespace FrontendSchemeRegistration.Application.ClassMaps;

using CsvHelper.Configuration;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public sealed class ExportOrganisationAllSubsidiariesRowMap : ClassMap<DTOs.Subsidiary.OrganisationSubsidiaryList.RelationshipResponseModel>
{
    public ExportOrganisationAllSubsidiariesRowMap()
    {
        Map(m => m.OrganisationName).Name("subsidiary_name");
        Map(m => m.OrganisationNumber).Name("subsidiary_id");
        Map(m => m.CompaniesHouseNumber).Name("companies_house_number");
    }
}