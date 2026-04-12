namespace eBuildingBlocks.Domain.Interfaces;

/// <summary>
/// Memory cache wrapper that prefixes keys with the current tenant when multi-tenancy is enabled.
/// </summary>
public interface ITenantMemoryCache
{
    TItem? Get<TItem>(string key);

    void Set<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow = null);

    void Remove(string key);
}
