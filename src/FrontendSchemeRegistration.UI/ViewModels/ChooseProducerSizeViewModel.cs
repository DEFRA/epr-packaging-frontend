namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Resources;

[ExcludeFromCodeCoverage]
public class ChooseProducerSizeViewModel
{
    [Required(ErrorMessageResourceName = "select_size_of_your_organisation", ErrorMessageResourceType = typeof(ErrorMessages))]
    public string? ProducerSize { get; set; }
}