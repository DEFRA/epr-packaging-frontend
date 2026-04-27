using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.ViewModels;

public class SubsidiaryLocationViewModel
{
    [Required]
    public Nation? UkNation { get; set; }
}
