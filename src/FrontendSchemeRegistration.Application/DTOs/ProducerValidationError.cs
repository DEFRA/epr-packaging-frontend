using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ProducerValidationError
{
    public string ProducerId { get; set; }

    public string SubsidiaryId { get; set; }

    public string ProducerType { get; set; }

    public string ProducerSize { get; set; }

    public string DataSubmissionPeriod { get; set; }

    public string WasteType { get; set; }

    public string? PackagingCategory { get; set; }

    public string MaterialType { get; set; }

    public string MaterialSubType { get; set; }

    public string? FromHomeNation { get; set; }

    public string? ToHomeNation { get; set; }

    public string? QuantityKg { get; set; }

    public string? QuantityUnits { get; set; }

    public string? QuantityType { get; set; }

    public string? TransitionalPackagingUnits { get; set; }

    public string? RecyclabilityRating { get; set; }

    public int RowNumber { get; set; }

    public string Issue { get; set; }

    public List<string> ErrorCodes { get; set; }
}