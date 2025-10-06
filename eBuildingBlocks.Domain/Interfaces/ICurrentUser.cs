namespace eBuildingBlocks.Domain.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserEmail { get; }
    public string? IPAddress { get; }
    public string? UserName { get; }
    public Guid TenantId { get; }
    public string UserAgent { get; }
}


public interface ITenantScope
{
    /// <summary>Temporarily override tenant for the current scope (e.g., background jobs).</summary>
    IDisposable Begin(Guid tenantId);
    Guid OverrideTenantId { get; }
}
