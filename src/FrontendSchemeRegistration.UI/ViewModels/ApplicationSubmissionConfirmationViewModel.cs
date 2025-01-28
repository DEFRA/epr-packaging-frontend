using FrontendSchemeRegistration.Application.DTOs.Submission;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class ApplicationSubmissionConfirmationViewModel : OrganisationNationViewModel
{
    public string ApplicationReferenceNumber { get; set; } = string.Empty;

    public string RegistrationReferenceNumber { get; set; } = string.Empty;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }
}