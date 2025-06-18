namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadBrandsSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string FileName { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public bool IsApprovedUser { get; set; }
    
    public bool IsResubmission { get; set; }

    public int? RegistrationYear { get; set; }
}
