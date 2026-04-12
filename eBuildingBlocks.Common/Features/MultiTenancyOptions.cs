using eBuildingBlocks.Common.utils;

namespace eBuildingBlocks.Common.Features;

public sealed class MultiTenancyOptions
{
    /// <summary>When true, a non-empty tenant must be resolved for each request or ambient scope; see <see cref="AllowDefaultTenantFallback"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>HTTP header carrying the tenant id (e.g. X-Tenant-Id). Resolved after JWT claim when both exist.</summary>
    public string HeaderName { get; set; } = "x-tenant-id";

    /// <summary>JWT / identity claim type for tenant id (not the header name).</summary>
    public string ClaimType { get; set; } = CustomClaimTypes.TenantId;

    /// <summary>
    /// When enabled and no tenant is resolved from scope, claim, or header, fall back to <see cref="DefaultTenantId"/>.
    /// Disable in production; prefer explicit header/claim or <see cref="ITenantScope.Begin"/>.
    /// </summary>
    public bool AllowDefaultTenantFallback { get; set; }

    public Guid DefaultTenantId { get; set; }

    /// <summary>
    /// When the user is authenticated, reject requests where the tenant header disagrees with the tenant claim.
    /// </summary>
    public bool ValidateHeaderAgainstClaims { get; set; } = true;
}
