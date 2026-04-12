using eBuildingBlocks.Common.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.Application.Middlewares;

/// <summary>
/// When multi-tenancy is enabled and the caller is authenticated, rejects requests where <see cref="MultiTenancyOptions.HeaderName"/>
/// disagrees with <see cref="MultiTenancyOptions.ClaimType"/> (mitigates header spoofing for JWT-based APIs).
/// </summary>
public sealed class TenantHeaderValidationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext httpContext, IOptionsMonitor<MultiTenancyOptions> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.ValidateHeaderAgainstClaims)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var claimTenant = user.Claims.FirstOrDefault(c => c.Type == options.ClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(claimTenant) || !Guid.TryParse(claimTenant, out var claimId))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var headerValue = httpContext.Request.Headers[options.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue) || !Guid.TryParse(headerValue, out var headerId))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        if (headerId != claimId)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
