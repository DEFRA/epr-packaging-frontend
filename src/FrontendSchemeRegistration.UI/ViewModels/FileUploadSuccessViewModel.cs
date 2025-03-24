namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string FileName { get; set; }

    public DateTime SubmissionDeadline { get; set; }
    
    public bool IsResubmission { get; set; }
}
