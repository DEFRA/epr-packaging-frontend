namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsViewModel : ViewModelWithOrganisationRole
{
    public DateTime SubmissionDeadline { get; set; }
}