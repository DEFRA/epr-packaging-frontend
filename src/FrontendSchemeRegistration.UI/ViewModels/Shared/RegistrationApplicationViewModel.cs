namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.Submission;
using Application.Enums;
using Services.RegistrationPeriods;
using Sessions;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationViewModel
{
    public bool IsComplianceScheme { get; set; }

    public string RegistrationYear { get; set; } = default!;
    public RegistrationTaskListStatus FileUploadStatus { get; set; } = RegistrationTaskListStatus.NotStarted;
    public RegistrationTaskListStatus PaymentViewStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;
    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;
    public string? ApplicationReferenceNumber { get; set; }
    public string? RegistrationReferenceNumber { get; set; }
    public bool IsResubmission { get; set; }
    public ApplicationStatusType ApplicationStatus { get; set; }
    public bool SummaryCardStyling { get; set; } = false;
    public string CurrentYear => (DateTime.Now.Year).ToString();
    public RegistrationJourney? RegistrationJourney { get; set; }
    public RegistrationWindow RegistrationWindow { get; set; }
    
    /// <summary>
    /// This property is used for direct producer flows. When the org is not a CSO, then it should not be null
    /// for the current registration period pattern. It may be null for legacy patterns
    /// </summary>
    public RegistrationWindow? SecondaryRegistrationWindow { get; set; }
    
    public string RegistrationApplicationLink
        => ApplicationStatus is
               ApplicationStatusType.FileUploaded
               or ApplicationStatusType.SubmittedAndHasRecentFileUpload
               or ApplicationStatusType.CancelledByRegulator
               or ApplicationStatusType.QueriedByRegulator
               or ApplicationStatusType.RejectedByRegulator
           || FileUploadStatus is
               RegistrationTaskListStatus.Pending
               or RegistrationTaskListStatus.Completed
            ? "RegistrationTaskList"
            : "ProducerRegistrationGuidance";
}
