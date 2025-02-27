using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class RegistrationTaskListViewModel
{
    public string OrganisationName { get; set; } = string.Empty;

    public string OrganisationNumber { get; set; } = string.Empty;

    public bool IsComplianceScheme { get; set; }
    
    public ApplicationStatusType ApplicationStatus { get; set; }

    public RegistrationTaskListStatus FileUploadStatus { get; set; }

    public RegistrationTaskListStatus PaymentViewStatus { get; set; }

    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; }
}