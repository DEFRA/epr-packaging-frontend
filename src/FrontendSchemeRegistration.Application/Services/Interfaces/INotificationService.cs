namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs.Notification;

public interface INotificationService
{
    Task<List<NotificationDto>?> GetCurrentUserNotifications(Guid organisationId, Guid userId);

    Task ResetCache(Guid organisationId, Guid userId);
}