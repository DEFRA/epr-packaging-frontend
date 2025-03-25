using System.Net;
using System.Net.Http.Json;

namespace FrontendSchemeRegistration.Application.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        private static readonly List<HttpStatusCode> PassThroughExceptions = new List<HttpStatusCode> { HttpStatusCode.PreconditionRequired };

        public static HttpResponseMessage EnsureSuccessStatusCodeDoNotConsumeExceptions(this HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
            {
                if (PassThroughExceptions.Contains(message.StatusCode) && message.StatusCode == System.Net.HttpStatusCode.PreconditionRequired)
                {
                    throw RaiseHttpRequestException(message);
                }
                return message.EnsureSuccessStatusCode();
            }

            return message;
        }

        private static HttpRequestException RaiseHttpRequestException(HttpResponseMessage message)
        {
            var body = message.Content.Headers.ContentLength > 0 ? message.Content.ReadFromJsonAsync<string>().Result : string.Empty;
            var reason = string.IsNullOrWhiteSpace(body) ? message.ReasonPhrase : body;
            reason = string.IsNullOrWhiteSpace(reason) ? $"Response status code does not indicate success: {(int)message.StatusCode}" : reason;

            return new HttpRequestException(reason, null, message.StatusCode);
        }
    }
}
