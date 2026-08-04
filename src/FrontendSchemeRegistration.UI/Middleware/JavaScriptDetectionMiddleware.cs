namespace FrontendSchemeRegistration.UI.Middleware;

using System.Net;
using System.Text;
using Application.Options;
using Constants;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

public class JavaScriptDetectionMiddleware
{
    public const string JavaScriptRequiredPath = "/javascript-required";

    // Lifetime (seconds) of the short-lived verification cookie. Only needs to survive
    // the immediate reload after the gate page runs, so a small value is fine.
    public const int VerifiedCookieMaxAgeSeconds = 60;

    private static readonly string[] BypassPaths =
    {
        JavaScriptRequiredPath,
        "/error",
        "/admin/health",
        "/signin-oidc",
        "/signout-oidc",
        "/signout-callback-oidc"
    };

    private readonly RequestDelegate _next;

    public JavaScriptDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IOptions<Application.Options.CookieOptions> cookieOptions,
        IOptions<AzureAdB2COptions> b2cOptions)
    {
        var cookieName = cookieOptions.Value.JsEnabledCookieName;

        if (string.IsNullOrEmpty(cookieName)
            || httpContext.Request.Cookies.ContainsKey(cookieName)
            || IsBypassPath(httpContext.Request.Path, b2cOptions.Value.SignedOutCallbackPath))
        {
            await _next(httpContext);
            return;
        }

        await WriteGatePageAsync(httpContext, cookieName, cookieOptions.Value.JsVerifiedCookieName);
    }

    private static bool IsBypassPath(PathString path, string? signedOutCallbackPath)
    {
        if (BypassPaths.Any(bypass => path.StartsWithSegments(bypass, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return !string.IsNullOrEmpty(signedOutCallbackPath)
               && path.StartsWithSegments(new PathString(signedOutCallbackPath), StringComparison.OrdinalIgnoreCase);
    }

    public static async Task WriteGatePageAsync(HttpContext httpContext, string enabledCookieName, string? verifiedCookieName)
    {
        var nonce = httpContext.Items[ContextKeys.ScriptNonceKey] as string ?? string.Empty;
        var basePath = httpContext.Request.PathBase.HasValue ? httpContext.Request.PathBase.Value : string.Empty;
        var cssHref = $"{basePath}/css/application.css";
        var noScriptUrl = $"{basePath}{JavaScriptRequiredPath}";
        var cookiePath = string.IsNullOrEmpty(basePath) ? "/" : basePath;
        var enabledCookieAttributes = $"{enabledCookieName}=1; path={cookiePath}; secure; samesite=lax";
        var verifiedCookieAttributes = string.IsNullOrEmpty(verifiedCookieName)
            ? null
            : $"{verifiedCookieName}=1; path={cookiePath}; secure; samesite=lax; max-age={VerifiedCookieMaxAgeSeconds}";

        var script = new StringBuilder()
            .Append("(function(){")
            .Append("if(!navigator.cookieEnabled){")
            .Append($"window.location.replace({JsString(noScriptUrl)});return;")
            .Append('}')
            .Append($"document.cookie={JsString(enabledCookieAttributes)};");
        if (verifiedCookieAttributes is not null)
        {
            script.Append($"document.cookie={JsString(verifiedCookieAttributes)};");
        }
        script.Append("window.location.replace(window.location.href);")
              .Append("})();");

        var html = new StringBuilder()
            .Append("<!DOCTYPE html>")
            .Append("<html lang=\"en\">")
            .Append("<head>")
            .Append("<meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<title>Loading…</title>")
            .Append($"<link rel=\"stylesheet\" href=\"{WebUtility.HtmlEncode(cssHref)}\">")
            .Append($"<noscript><meta http-equiv=\"refresh\" content=\"0; url={WebUtility.HtmlEncode(noScriptUrl)}\"></noscript>")
            .Append($"<script nonce=\"{WebUtility.HtmlEncode(nonce)}\">")
            .Append(script)
            .Append("</script>")
            .Append("</head>")
            .Append("<body>")
            .Append("<div class=\"js-detection-wrapper\">")
            .Append("<div class=\"js-detection-spinner\" role=\"status\" aria-live=\"polite\" aria-label=\"Loading\"></div>")
            .Append("<p class=\"js-detection-label\">Loading…</p>")
            .Append("</div>")
            .Append("</body>")
            .Append("</html>")
            .ToString();

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        httpContext.Response.Headers[HeaderNames.Pragma] = "no-cache";

        await httpContext.Response.WriteAsync(html, Encoding.UTF8);
    }

    private static string JsString(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("<", "\\u003C")
            .Replace(">", "\\u003E");
        return $"'{escaped}'";
    }
}
