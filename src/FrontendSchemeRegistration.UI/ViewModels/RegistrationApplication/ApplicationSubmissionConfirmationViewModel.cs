using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

using Application.Enums;

[ExcludeFromCodeCoverage]
public class ApplicationSubmissionConfirmationViewModel : OrganisationNationViewModel
{
    public string ApplicationReferenceNumber { get; set; } = string.Empty;

    public string RegistrationReferenceNumber { get; set; } = string.Empty;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }
    
    public bool IsComplianceScheme { get; set; }
    public bool isResubmission { get; set; }

    public int RegistrationYear { get; set; }
    public RegistrationJourney? RegistrationJourney { get; set; }
    public bool ShowRegistrationCaption => RegistrationJourney != null;
}