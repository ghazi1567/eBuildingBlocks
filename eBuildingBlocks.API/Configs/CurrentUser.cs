using eBuildingBlocks.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BuildingBlocks.API.Configs;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserAgent => httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? string.Empty;
    public string IPAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    public string UserName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;


    public Guid TenantId
    {
        get
        {
            var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }
    }

}
