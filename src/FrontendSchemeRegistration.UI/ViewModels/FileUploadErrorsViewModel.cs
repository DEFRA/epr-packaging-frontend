namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class FileUploadErrorsViewModel
{
    public int ErrorCount { get; set; }

    public string? OrganisationRole { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public Guid SubmissionId { get; set; }

    public int? RegistrationYear { get; set; }
    public RegistrationJourney? RegistrationJourney { get; set; }
    public bool ShowRegistrationCaption => RegistrationJourney != null && RegistrationYear != null;
}