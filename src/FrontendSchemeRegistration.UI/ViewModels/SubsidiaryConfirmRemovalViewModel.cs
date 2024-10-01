using FrontendSchemeRegistration.UI.Resources;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SubsidiaryConfirmRemovalViewModel
    {
        [Required(ErrorMessageResourceName = "Select_yes_if_you_would_like_to_remove_this_member_from_your_account", ErrorMessageResourceType = typeof(ErrorMessages))]
        public YesNoAnswer? SelectedConfirmRemoval { get; set; }
        
        public string SubsidiaryName { get; set; }
        
        /// <remarks>
        /// Actually Id.
        /// </remarks>
        public Guid SubsidiaryExternalId { get; set; }

        public Guid ParentOrganisationExternalId { get; set; }
    }
}
