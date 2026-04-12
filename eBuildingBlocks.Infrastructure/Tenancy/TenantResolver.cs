using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Exceptions;
using eBuildingBlocks.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eBuildingBlocks.Infrastructure.Tenancy;

/// <summary>
/// Resolves tenant from <see cref="ITenantScope"/> (AsyncLocal), then JWT claim, then HTTP header, then optional default.
/// </summary>
public sealed class TenantResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<MultiTenancyOptions> multiTenancyOptions,
    ITenantScope scope) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IOptionsMonitor<MultiTenancyOptions> _multiTenancyOptions = multiTenancyOptions;
    private readonly ITenantScope _scope = scope;

    public string UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? string.Empty;

    public string IPAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    public string UserName =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public string? UserEmail =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public Guid TenantId
    {
        get
        {
            var mt = _multiTenancyOptions.CurrentValue;
            if (!mt.Enabled)
                return Guid.Empty;

            var overrideId = _scope.OverrideTenantId;
            if (overrideId != Guid.Empty)
                return overrideId;

            var ctx = _httpContextAccessor.HttpContext;

            // Prefer signed-in token over spoofable header when both can apply.
            var claimValue = ctx?.User?.FindFirstValue(mt.ClaimType);
            if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out var claimTenantId))
                return claimTenantId;

            var header = ctx?.Request.Headers[mt.HeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out var headerTenantId))
                return headerTenantId;

            if (mt.AllowDefaultTenantFallback && mt.DefaultTenantId != Guid.Empty)
                return mt.DefaultTenantId;

            throw new TenantResolutionException(
                $"Multi-tenancy is enabled but no tenant was resolved. Provide JWT claim '{mt.ClaimType}', HTTP header '{mt.HeaderName}', " +
                $"or call {nameof(ITenantScope)}.{nameof(ITenantScope.Begin)} with a non-empty tenant id. " +
                $"Alternatively set {nameof(MultiTenancyOptions.AllowDefaultTenantFallback)} and {nameof(MultiTenancyOptions.DefaultTenantId)} for development only.");
        }
    }
}
