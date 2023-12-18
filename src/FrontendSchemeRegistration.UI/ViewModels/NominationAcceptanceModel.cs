namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;

public class NominationAcceptanceModel
{
    public Guid? EnrolmentId { get; set; }

    public string? NominatorFullName { get; set; }

    [MaxLength(200, ErrorMessage = "ConfirmPermissionSubmitData.FullNameMaxLengthError")]
    [Required(ErrorMessage = "ConfirmPermissionSubmitData.FullNameError")]
    public string? NomineeFullName { get; set; }

    public string? OrganisationName { get; set; }
}