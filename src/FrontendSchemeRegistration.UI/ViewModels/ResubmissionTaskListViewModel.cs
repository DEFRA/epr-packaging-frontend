using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class ResubmissionTaskListViewModel
{
    public string OrganisationName { get; set; } = string.Empty;

    public string OrganisationNumber { get; set; } = string.Empty;

    public bool IsComplianceScheme { get; set; }

    public ResubmissionTaskListStatus FileUploadStatus { get; set; }

    public ResubmissionTaskListStatus PaymentViewStatus { get; set; }

    public ResubmissionTaskListStatus AdditionalDetailsStatus { get; set; }

    public bool FileReachedSynapse { get; set; }

    public ApplicationStatusType ApplicationStatus { get; set; }

    public string AppReferenceNumber { get; set; }

    public bool IsSubmitted { get; set; }

    public bool? IsResubmissionInProgress { get; set; }

    public bool? IsResubmissionComplete { get; set; }

    public bool ResubmissionApplicationSubmitted { get; set; }
}