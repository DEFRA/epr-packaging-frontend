namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CompanyDetailsConfirmationModel : ViewModelWithOrganisationRole
{
    public string? SubmittedDate { get; set; }

    public string? SubmissionTime { get; set; }

    public string? SubmittedBy { get; set; }
}