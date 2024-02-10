namespace FrontendSchemeRegistration.Application.ClassMaps;

using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration;
using DTOs;

[ExcludeFromCodeCoverage]
public class RegistrationErrorReportRowMap : ClassMap<RegistrationErrorReportRow>
{
    public RegistrationErrorReportRowMap()
    {
        Map(m => m.Row).Name("Row");
        Map(m => m.OrganisationId).Name("Org ID");
        Map(m => m.SubsidiaryId).Name("Subsidiary ID");
        Map(m => m.Column).Name("Column");
        Map(m => m.ColumnName).Name("Column name");
        Map(m => m.Error).Name("Error");
    }
}