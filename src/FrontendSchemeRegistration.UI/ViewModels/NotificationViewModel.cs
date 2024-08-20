namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.Constants;
using Application.DTOs.Notification;

public class NotificationViewModel
{
    public bool HasNominatedNotification { get; set; }

    public bool HasApprovedPersonNominatedNotification { get; set; }

    public string? NominatedEnrolmentId { get; set; }

    public string? NominatedApprovedPersonEnrolmentId { get; set; }

    public bool HasPendingNotification { get; set; }

    public void BuildFromNotificationList(List<NotificationDto> notificationList)
    {
        NotificationDto delegatedPersonPendingApproval = null;
        NotificationDto delegatedPersonNomination = null;
        NotificationDto approvedPersonNomination = null;

        if (notificationList != null)
        {
            delegatedPersonNomination = notificationList.Find(n => n.Type == NotificationTypes.Packaging.DelegatedPersonNomination);

            if (delegatedPersonNomination == null)
            {
                delegatedPersonPendingApproval = notificationList.Find(n => n.Type == NotificationTypes.Packaging.DelegatedPersonPendingApproval);
            }

            approvedPersonNomination = notificationList.Find(n => n.Type == NotificationTypes.Packaging.ApprovedsPersonNomination);
        }

        if (delegatedPersonNomination != null && !delegatedPersonNomination.Data.Any(d => d.Key == "EnrolmentId"))
        {
            throw new ArgumentException("Delegated person nomination missing 'EnrolmentId'", nameof(notificationList));
        }

        HasNominatedNotification = delegatedPersonNomination != null;
        HasApprovedPersonNominatedNotification = approvedPersonNomination != null;
        NominatedEnrolmentId = delegatedPersonNomination != null ? delegatedPersonNomination.Data.First(d => d.Key == "EnrolmentId").Value : string.Empty;
        NominatedApprovedPersonEnrolmentId = approvedPersonNomination != null ? approvedPersonNomination.Data.First(d => d.Key == "EnrolmentId").Value : string.Empty;
        HasPendingNotification = delegatedPersonPendingApproval != null;
    }
}