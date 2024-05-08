namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadViewModel : ViewModelWithOrganisationRole
{
    public List<string> ExceptionErrorCodes { get; set; } = new ();
}
