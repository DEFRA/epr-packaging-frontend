using CsvHelper.Configuration;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.ClassMaps;

[ExcludeFromCodeCoverage]
public class SubsidiaryUploadErrorRowMap : ClassMap<SubsidiaryUploadErrorRow>
{
    public SubsidiaryUploadErrorRowMap()
    {
        Map(m => m.OrganisationId).Name("organisation_id");
        Map(m => m.SubsidiaryId).Name("subsidiary_id");
        Map(m => m.OrganisationName).Name("organisation_name");
        Map(m => m.CompaniesHouseNumber).Name("companies_house_number");
        Map(m => m.ParentChild).Name("parent_child");
        Map(m => m.FranchiseeLicenseeTenant).Name("franchisee_licensee_tenant");
        Map(m => m.RowNumber).Name("Row Number");
        Map(m => m.Issue).Name("Issue");
        Map(m => m.Message).Name("Message");
    }
}
