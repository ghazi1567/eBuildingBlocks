using eBuildingBlocks.API.Features;
using eBuildingBlocks.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BuildingBlocks.API.Startup;
public sealed class TenantScope : ITenantScope
{
    private static readonly AsyncLocal<Guid> _ambient = new();
    public Guid OverrideTenantId => _ambient.Value;


    public IDisposable Begin(Guid tenantId)
    {
        var previous = _ambient.Value;
        _ambient.Value = tenantId;
        return new Scope(() => _ambient.Value = previous);
    }

    private sealed class Scope(Action dispose) : IDisposable { public void Dispose() => dispose(); }
}

public class TenantResolver : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancyOptions multiTenancy;
    private readonly ITenantScope _scope;
    public TenantResolver(IHttpContextAccessor httpContextAccessor, IConfiguration cfg, ITenantScope scope)
    {
        _httpContextAccessor = httpContextAccessor;
        multiTenancy = FeatureGate.Get<MultiTenancyOptions>(cfg, "Features:MultiTenancy") ?? new MultiTenancyOptions();
        _scope = scope;
    }
    public string UserAgent => _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? string.Empty;
    public string IPAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    public string UserName => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string? UserEmail => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;

    public bool Enabled => multiTenancy.Enabled;

    public Guid TenantId
    {
        get
        {
            if (!Enabled) return Guid.Empty;

            // 1) explicit override (background jobs/command handlers)
            var overrideId = _scope.OverrideTenantId;
            if (overrideId != Guid.Empty) return overrideId;

            // 2) HTTP header
            var ctx = _httpContextAccessor.HttpContext;
            var header = ctx?.Request.Headers[multiTenancy.HeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header)) return Guid.Parse(header);

            var tenantIdInClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(multiTenancy.HeaderName) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(tenantIdInClaim)) return Guid.Parse(tenantIdInClaim);

            // 3) fallback default
            return multiTenancy.DefaultTenantId;
        }
    }

}
