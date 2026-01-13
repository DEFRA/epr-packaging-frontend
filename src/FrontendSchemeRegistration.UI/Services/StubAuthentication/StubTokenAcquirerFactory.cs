using System.Security.Claims;
using Microsoft.Identity.Abstractions;

namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

public class StubTokenAcquirerFactory : ITokenAcquirerFactory
{
    private readonly StubAuthorizationHeaderProvider _provider = new();
    private readonly ITokenAcquirer _stubTokenAcquirer = new StubTokenAcquirer();

    public IAuthorizationHeaderProvider GetAuthorizationHeaderProvider(ClaimsPrincipal? user = null)
    {
        return _provider;
    }

    public ITokenAcquirer GetTokenAcquirer(IdentityApplicationOptions identityApplicationOptions)
    {
        // Return a stub token acquirer regardless of options
        return _stubTokenAcquirer;
    }

    public ITokenAcquirer GetTokenAcquirer(string optionName = "")
    {
        // Return a stub token acquirer regardless of option name
        return _stubTokenAcquirer;
    }
}

// Minimal stub implementation that provides stubbed authorization headers.
internal sealed class StubTokenAcquirer : ITokenAcquirer
{
    private readonly StubAuthorizationHeaderProvider _provider = new();

    private static AcquireTokenResult CreateResult(string token)
    {
        // Use reflection to be resilient to different package versions
        var type = typeof(AcquireTokenResult);
        var instance = (AcquireTokenResult)Activator.CreateInstance(type, nonPublic: true)!;
        var accessTokenProp = type.GetProperty("AccessToken");
        accessTokenProp?.SetValue(instance, token);
        var expiresOnProp = type.GetProperty("ExpiresOn");
        expiresOnProp?.SetValue(instance, DateTimeOffset.UtcNow.AddHours(1));
        return instance;
    }

    public Task<AcquireTokenResult> GetTokenForUserAsync(
        IEnumerable<string> scopes,
        AcquireTokenOptions? options = null,
        ClaimsPrincipal? user = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CreateResult("stub-access-token"));

    public Task<AcquireTokenResult> GetTokenForAppAsync(
        string scope,
        AcquireTokenOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CreateResult("stub-app-access-token"));
}
