namespace eBuildingBlocks.Common.Features;

public sealed class MultiTenancyOptions
{
    public bool Enabled { get; set; } = false;
    public string HeaderName { get; set; } = "x-tenant-id";
    public Guid DefaultTenantId { get; set; }
}
