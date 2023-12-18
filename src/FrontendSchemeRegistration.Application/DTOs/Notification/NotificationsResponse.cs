using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Notification
{
    [ExcludeFromCodeCoverage]
    public class NotificationsResponse
    {
        public List<NotificationDto> Notifications { get; set; }
    }
}
