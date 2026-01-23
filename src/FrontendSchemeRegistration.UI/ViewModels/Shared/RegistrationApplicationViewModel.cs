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
    /// This property is used in direct producer flows as a proxy for whether or not the small direct producer window has opened. It is assumed
    /// that the direct large producer window's dates are prior to the small producer window's dates, so if we don't have a property here, then
    /// the direct small producer window has not opened (or this model is being used in the context of a CSO)  
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
