using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Notification
{
    public class NotificationDto
    {
        public string Type { get; set; }

        public ICollection<KeyValuePair<string, string>> Data { get; set; }
    }
}
