using System.ComponentModel.DataAnnotations;

namespace eBuildingBlocks.Domain.Models;

public interface Entity<TKey> : IEntity, ITenantEntity
{
    [Key]
    TKey Id { get; set; }
}
public interface IEntity
{
}

public interface ITenantEntity
{
    Guid TenantId { get; set; } // Link user to tenant
}