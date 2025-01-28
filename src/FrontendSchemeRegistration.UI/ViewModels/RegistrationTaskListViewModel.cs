using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.UI.Sessions;
using static FrontendSchemeRegistration.Application.DTOs.Submission.RegistrationApplicationDetails;

namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationTaskListViewModel
{
    public string OrganisationName { get; set; } = string.Empty;

    public string OrganisationNumber { get; set; } = string.Empty;

    public RegistrationTaskListStatus FileUploadStatus { get; set; }

    public RegistrationTaskListStatus PaymentViewStatus { get; set; }

    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; }

    public bool FileReachedSynapse { get; set; }
    
    public ApplicationStatusType ApplicationStatus { get; set; }
}