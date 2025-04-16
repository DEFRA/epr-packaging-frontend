using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ErrorReportRow
{
    public ErrorReportRow()
    {
    }

    public ErrorReportRow(ProducerValidationError producerValidationError, string issue, string message)
    {
        ProducerId = producerValidationError.ProducerId;
        SubsidiaryId = producerValidationError.SubsidiaryId;
        ProducerType = producerValidationError.ProducerType;
        DataSubmissionPeriod = producerValidationError.DataSubmissionPeriod;
        ProducerSize = producerValidationError.ProducerSize;
        WasteType = producerValidationError.WasteType;
        PackagingCategory = producerValidationError.PackagingCategory;
        MaterialType = producerValidationError.MaterialType;
        MaterialSubType = producerValidationError.MaterialSubType;
        FromHomeNation = producerValidationError.FromHomeNation;
        ToHomeNation = producerValidationError.ToHomeNation;
        QuantityKg = producerValidationError.QuantityKg;
        QuantityUnits = producerValidationError.QuantityUnits;
        TransitionalPackagingUnits = producerValidationError.TransitionalPackagingUnits;
        RecyclabilityRating = producerValidationError.RecyclabilityRating;
        RowNumber = producerValidationError.RowNumber;
        Issue = issue;
        Message = message;
    }

    public string ProducerId { get; set; }

    public string SubsidiaryId { get; set; }

    public string ProducerType { get; set; }

    public string DataSubmissionPeriod { get; set; }

    public string ProducerSize { get; set; }

    public string WasteType { get; set; }

    public string? PackagingCategory { get; set; }

    public string MaterialType { get; set; }

    public string MaterialSubType { get; set; }

    public string? FromHomeNation { get; set; }

    public string? ToHomeNation { get; set; }

    public string? QuantityKg { get; set; }

    public string? QuantityUnits { get; set; }

    public string? TransitionalPackagingUnits { get; set; }

    public string? RecyclabilityRating { get; set; }

    public int RowNumber { get; set; }

    public string Issue { get; set; }

    public string Message { get; set; }
}