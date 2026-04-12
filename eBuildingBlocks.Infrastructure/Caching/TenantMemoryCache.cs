using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.Infrastructure.Caching;

public sealed class TenantMemoryCache(
    IMemoryCache inner,
    ICurrentUser currentUser,
    IOptionsMonitor<MultiTenancyOptions> multiTenancyOptions) : ITenantMemoryCache
{
    private readonly IMemoryCache _inner = inner;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IOptionsMonitor<MultiTenancyOptions> _multiTenancyOptions = multiTenancyOptions;

    public TItem? Get<TItem>(string key) => _inner.Get<TItem>(Prefix(key));

    public void Set<TItem>(string key, TItem value, TimeSpan? absoluteExpirationRelativeToNow = null)
    {
        var opts = new MemoryCacheEntryOptions();
        if (absoluteExpirationRelativeToNow is { } exp)
            opts.SetAbsoluteExpiration(exp);
        _inner.Set(Prefix(key), value, opts);
    }

    public void Remove(string key) => _inner.Remove(Prefix(key));

    private string Prefix(string key)
    {
        var mt = _multiTenancyOptions.CurrentValue;
        if (!mt.Enabled || _currentUser.TenantId == Guid.Empty)
            return key;
        return FormattableString.Invariant($"{_currentUser.TenantId:N}:{key}");
    }
}
