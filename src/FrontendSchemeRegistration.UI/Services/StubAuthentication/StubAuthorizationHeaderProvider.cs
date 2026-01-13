using System.Security.Claims;
using System.Threading;
using Microsoft.Identity.Abstractions;

namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

public class StubAuthorizationHeaderProvider : IAuthorizationHeaderProvider
{
    public Task<string> CreateAuthorizationHeaderAsync(
        IEnumerable<string> scopes,
        AuthorizationHeaderProviderOptions? options = null,
        ClaimsPrincipal? user = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Bearer stub-access-token");

    public Task<string> CreateAuthorizationHeaderForUserAsync(
        IEnumerable<string> scopes,
        AuthorizationHeaderProviderOptions? options = null,
        ClaimsPrincipal? user = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Bearer stub-access-token");

    public Task<string> CreateAuthorizationHeaderForAppAsync(
        string scope,
        AuthorizationHeaderProviderOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Bearer stub-app-access-token");
}
