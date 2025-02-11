namespace FrontendSchemeRegistration.Application.ClassMaps;

using CsvHelper.Configuration;
using DTOs.Subsidiary;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public sealed class ExportOrganisationSubsidiariesRowMap : ClassMap<ExportOrganisationSubsidiariesResponseModel>
{
    public ExportOrganisationSubsidiariesRowMap(bool includeSubsidiaryJoinerAndLeaverColumns)
    {
        Map(m => m.OrganisationId).Name("organisation_id");
        Map(m => m.SubsidiaryId).Name("subsidiary_id");
        Map(m => m.OrganisationName).Name("organisation_name");
        Map(m => m.CompaniesHouseNumber).Name("companies_house_number");

        if (includeSubsidiaryJoinerAndLeaverColumns)
        {
            Map(m => m.JoinerDate).Name("joiner_date");
            Map(m => m.ReportingType).Name("reporting_type");
        }
    }
}