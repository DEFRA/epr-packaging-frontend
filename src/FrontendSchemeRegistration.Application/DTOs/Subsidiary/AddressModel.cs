using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

[ExcludeFromCodeCoverage]
public class AddressModel
{
    [MaxLength(100)]
    public string? SubBuildingName { get; set; }

    [MaxLength(100)]
    public string? BuildingName { get; set; }

    [MaxLength(50)]
    public string? BuildingNumber { get; set; }

    [MaxLength(100)]
    public string? Street { get; set; }

    [MaxLength(100)]
    public string? Locality { get; set; }

    [MaxLength(100)]
    public string? DependentLocality { get; set; }

    [MaxLength(70)]
    public string? Town { get; set; }

    [MaxLength(50)]
    public string? County { get; set; }

    [MaxLength(15)]
    public string? Postcode { get; set; }

    [MaxLength(54)]
    public string? Country { get; set; }
}
