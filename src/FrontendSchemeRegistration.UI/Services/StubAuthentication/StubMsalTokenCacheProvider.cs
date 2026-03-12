using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class StubMsalTokenCacheProvider : IMsalTokenCacheProvider
{
    public void Initialize(ITokenCache tokenCache)
    {
        // No-op for stub
    }

    public Task InitializeAsync(ITokenCache tokenCache)
    {
        // No-op for stub
        return Task.CompletedTask;
    }

    public Task ClearAsync(string cacheKey)
    {
        // No-op for stub
        return Task.CompletedTask;
    }
}
