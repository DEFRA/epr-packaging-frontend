namespace FrontendSchemeRegistration.Application.ClassMaps;

using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration;
using DTOs;

[ExcludeFromCodeCoverage]
public class ErrorReportRowMap : ClassMap<ErrorReportRow>
{
    public ErrorReportRowMap(List<string> optionalHeaders)
    {
        Map(m => m.ProducerId).Name("organisation_id");
        Map(m => m.SubsidiaryId).Name("subsidiary_id");
        Map(m => m.ProducerSize).Name("organisation_size");
        Map(m => m.DataSubmissionPeriod).Name("submission_period");
        Map(m => m.ProducerType).Name("packaging_activity");
        Map(m => m.WasteType).Name("packaging_type");
        Map(m => m.PackagingCategory).Name("packaging_class");
        Map(m => m.MaterialType).Name("packaging_material");
        Map(m => m.MaterialSubType).Name("packaging_material_subtype");
        Map(m => m.FromHomeNation).Name("from_country");
        Map(m => m.ToHomeNation).Name("to_country");
        Map(m => m.QuantityKg).Name("packaging_material_weight");
        Map(m => m.QuantityUnits).Name("packaging_material_units");
        if (optionalHeaders.Contains("transitional_packaging_units"))
        {
            Map(m => m.TransitionalPackagingUnits).Name("transitional_packaging_units");
        }
        if (optionalHeaders.Contains("ram_rag_rating"))
        {
            Map(m => m.RecyclabilityRating).Name("ram_rag_rating");
        }
        Map(m => m.RowNumber).Name("Row Number");
        Map(m => m.Issue).Name("Issue");
        Map(m => m.Message).Name("Message");
    }
}