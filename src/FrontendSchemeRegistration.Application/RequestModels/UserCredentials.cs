using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels;

[ExcludeFromCodeCoverage]
public class UserCredentials
{
    public UserCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; }

    public string Password { get; }
}