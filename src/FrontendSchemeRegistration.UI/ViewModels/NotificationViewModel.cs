namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.Constants;
using Application.DTOs.Notification;

public class NotificationViewModel
{
    public bool HasNominatedNotification { get; set; }

    public string? NominatedEnrolmentId { get; set; }

    public bool HasPendingNotification { get; set; }

    public void BuildFromNotificationList(List<NotificationDto> notificationList)
    {
        NotificationDto delegatedPersonPendingApproval = null;
        NotificationDto delegatedPersonNomination = null;

        if (notificationList != null)
        {
            delegatedPersonNomination = notificationList.FirstOrDefault(n => n.Type == NotificationTypes.Packaging.DelegatedPersonNomination);

            if (delegatedPersonNomination == null)
            {
                delegatedPersonPendingApproval = notificationList.FirstOrDefault(n => n.Type == NotificationTypes.Packaging.DelegatedPersonPendingApproval);
            }
        }

        if (delegatedPersonNomination != null && !delegatedPersonNomination.Data.Any(d => d.Key == "EnrolmentId"))
        {
            throw new ArgumentException("Delegated person nomination missing 'EnrolmentId'", nameof(notificationList));
        }

        HasNominatedNotification = delegatedPersonNomination != null;
        NominatedEnrolmentId = delegatedPersonNomination != null ? delegatedPersonNomination.Data.First(d => d.Key == "EnrolmentId").Value : string.Empty;
        HasPendingNotification = delegatedPersonPendingApproval != null;
    }
}