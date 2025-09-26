using eBuildingBlocks.Domain.Interfaces;

namespace eBuildingBlocks.Domain.Models
{
    // Optional: if most entities are tenant-scoped, use this base; otherwise just implement ITenantEntity directly.
    public abstract class TenantEntity<TKey> : AuditableEntity<TKey>, ITenantEntity
        where TKey : IEquatable<TKey>
    {
        public Guid TenantId { get; set; } = default!;
    }
}
