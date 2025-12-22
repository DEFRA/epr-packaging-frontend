namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class FileUploadingViewModel
{
    public string? SubmissionId { get; set; }
    public int? RegistrationYear { get; set; }

    public bool ShowRegistrationCaption { get; set; } = false;

    public RegistrationJourney? RegistrationJourney { get; set; }
}