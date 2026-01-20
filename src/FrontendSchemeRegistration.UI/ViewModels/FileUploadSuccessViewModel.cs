namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class FileUploadSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string FileName { get; set; }

    public DateTime SubmissionDeadline { get; set; }
    
    public bool IsResubmission { get; set; }

    public int? RegistrationYear { get; set; }

    public bool ShowRegistrationCaption => RegistrationJourney != null;

    public RegistrationJourney? RegistrationJourney { get; set; }
    public string OrganisationName { get; set; }
}
