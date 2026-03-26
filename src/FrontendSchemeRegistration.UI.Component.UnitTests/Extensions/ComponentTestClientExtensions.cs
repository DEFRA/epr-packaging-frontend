namespace FrontendSchemeRegistration.UI.Component.UnitTests.Extensions;

using Infrastructure;

public static class ComponentTestClientExtensions
{
    public static async Task AuthenticateDefaultUser(this ComponentTestClient client)
    {
        var formData = new Dictionary<string, string>
        {
            { "Email", "test@test.com" }, 
            { "UserId", "9e4da0ed-cdff-44a1-8ae0-cef7f22b914b" }, 
            { "ReturnUrl", "/home" }
        };
        
        await client.PostAsync("/services/account-details", formData);
    }
}