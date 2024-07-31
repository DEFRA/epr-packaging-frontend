using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryCompaniesHouseNumberViewModel
{
    [Required(ErrorMessage = "CompaniesHouseNumber.ErrorMessage")]
    [MaxLength(8, ErrorMessage = "CompaniesHouseNumber.LengthErrorMessage")]
    public string? CompaniesHouseNumber { get; set; }
}
