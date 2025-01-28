using FrontendSchemeRegistration.UI.Resources;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SubsidiaryUnsuccessfulUploadDecisionViewModel
    {
        [Required(ErrorMessageResourceName = "select_yes_if_you_want_to_upload_a_new_subsidiary_file", ErrorMessageResourceType = typeof(ErrorMessages))]
        public bool? UploadNewFile {  get; set; }
    }
}
