using FrontendSchemeRegistration.UI.Resources;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SubsidiaryConfirmRemovalViewModel
    {
        [Required(ErrorMessageResourceName = "select_yes_to_remove_this_subsidiary", ErrorMessageResourceType = typeof(ErrorMessages))]
        public YesNoAnswer? SelectedConfirmRemoval { get; set; }
        
        public string SubsidiaryName { get; set; }
        
        /// <remarks>
        /// Actually Id.
        /// </remarks>
        public Guid SubsidiaryExternalId { get; set; }

        public Guid ParentOrganisationExternalId { get; set; }

        public string SubsidiaryReference { get; set; }
    }
}
