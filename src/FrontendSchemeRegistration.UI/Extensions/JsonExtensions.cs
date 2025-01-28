namespace FrontendSchemeRegistration.UI.Extensions;

using System.Text;
using System.Text.Json;


public static class JsonExtensions
{
    public static StringContent ToJsonContent(this object parameters, JsonSerializerOptions options = null)
    {
        var jsonContent = JsonSerializer.Serialize(parameters, options);
        return new StringContent(jsonContent, Encoding.UTF8, "application/json");
    }
}