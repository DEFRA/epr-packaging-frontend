namespace FrontendSchemeRegistration.Application.Services
{
    public class ServiceClientBase
    {
        public static string BuildUrlWithQueryString(object dto)
        {
            var properties = dto.GetType().GetProperties()
                .Where(p => p.GetValue(dto, null) != null)
                .Select(p => p.Name + "=" + Uri.EscapeDataString(p.GetValue(dto, null).ToString()));
            return "?" + string.Join("&", properties);
        }
    }
}