using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class RemovalTellUsMoreViewModel
{
    [MaxLength(200, ErrorMessage = "RemovalTellUsMore.Error200")]
    [Required(ErrorMessage = "RemovalTellUsMore.Error")]
    public string TellUsMore { get; set; }

    public string SelectedReasonForRemoval { get; set; }
}
