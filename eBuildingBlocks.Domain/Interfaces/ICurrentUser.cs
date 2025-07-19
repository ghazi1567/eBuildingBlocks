namespace eBuildingBlocks.Domain.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    public string? IPAddress { get; }
    public string? UserName { get; }
    public Guid TenantId { get; }
    public string UserAgent { get; }
}
