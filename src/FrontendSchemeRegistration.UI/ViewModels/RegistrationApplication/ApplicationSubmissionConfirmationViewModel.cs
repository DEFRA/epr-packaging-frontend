using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class ApplicationSubmissionConfirmationViewModel : OrganisationNationViewModel
{
    public string ApplicationReferenceNumber { get; set; } = string.Empty;

    public string RegistrationReferenceNumber { get; set; } = string.Empty;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }
    
    public bool IsComplianceScheme { get; set; }
    public bool isResubmission { get; set; }
}