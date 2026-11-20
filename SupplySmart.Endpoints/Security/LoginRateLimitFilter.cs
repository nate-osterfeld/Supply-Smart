using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Security;

public sealed class LoginRateLimitFilter(LoginAttemptLimiter limiter) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var dto = context.GetArgument<LoginDto>(0);
        // Malformed input follows normal validation without allocating a limiter entry.
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), null, validateAllProperties: true))
            return await next(context);

        if (!limiter.TryAcquire(dto.Email, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
            return Results.Json(new { message = "Too many sign-in attempts. Please try again shortly." },
                statusCode: StatusCodes.Status429TooManyRequests);
        }
        return await next(context);
    }
}
