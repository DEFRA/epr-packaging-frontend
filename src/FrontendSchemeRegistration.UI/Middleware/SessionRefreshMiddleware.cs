namespace FrontendSchemeRegistration.UI.Middleware;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Middleware that refreshes the HTTP session whenever an authenticated user makes a request.
/// This ensures the session remains alive as long as the user is authenticated.
/// </summary>
public class SessionRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public SessionRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        // Refresh session if user is authenticated and session is available
        if (context.User.Identity?.IsAuthenticated == true && context.Session.IsAvailable)
        {
            // Writing to the session resets the idle timer
            // Redis cache TTL is only reset on a write/save
            context.Session.SetString("_LastActivity", DateTime.UtcNow.ToString("O"));
        }

        await _next(context);
    }
}

