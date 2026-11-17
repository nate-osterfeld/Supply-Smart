using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Security;

// The browser credential routes carry this metadata. API clients still use Authorization: Bearer.
public sealed class BrowserSession
{
    public const string CookieName = "__Host-SupplySmart-Session";
    private const string RequestHeader = "X-SupplySmart-CSRF";

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/",
        IsEssential = true
    };

    public static void CompleteSignIn(HttpContext context, AuthResponseDto result)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.GetEndpoint()?.Metadata.GetMetadata<BrowserSession>() is null) return;

        var options = CookieOptions();
        options.MaxAge = AuthService.TokenLifetime;
        context.Response.Cookies.Append(CookieName, result.Token!, options);
        result.Token = null;
    }

    public static void Clear(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.Cookies.ContainsKey(CookieName))
            context.Response.Cookies.Delete(CookieName, CookieOptions());
    }

    public static async Task ProtectWrites(HttpContext context, RequestDelegate next, IConfiguration configuration)
    {
        var request = context.Request;
        var startingSession = context.GetEndpoint()?.Metadata.GetMetadata<BrowserSession>() is not null;
        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method) ||
            (!startingSession && !request.Cookies.ContainsKey(CookieName)))
        {
            await next(context);
            return;
        }

        // Browsers supply Origin on writes. Only the configured SPA may start a session.
        // Subsequent writes must also identify the particular session that displayed the form.
        var sourceOrigin = request.Headers.Origin.ToString();
        var sameOrigin = string.Equals(sourceOrigin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
        var configuredOrigin = Uri.TryCreate(configuration["FrontendUrl"], UriKind.Absolute, out var publicUrl) &&
            string.Equals(sourceOrigin, publicUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
        var expectedHeader = startingSession ? "start-session" : context.User.FindFirstValue("jti");
        if ((!sameOrigin && !configuredOrigin) || string.IsNullOrEmpty(expectedHeader) ||
            !string.Equals(request.Headers[RequestHeader].ToString(), expectedHeader, StringComparison.Ordinal))
        {
            context.Response.Headers.CacheControl = "no-store";
            await Results.Json(new { message = "Your session could not be verified. Reload the page and try again." },
                statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }
        await next(context);
    }
}
