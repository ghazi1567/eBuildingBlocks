using eBuildingBlocks.Common.utils;

namespace eBuildingBlocks.Domain.Models;

public abstract class BaseEntity : Entity<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; }
    public string DeletedBy { get; private set; } = string.Empty;
    public Guid Id { get; set; }
    public Guid TenantId { get; set; } // Link user to tenant
    protected BaseEntity()
    {
        if (Id == Guid.Empty)
        {
            Id = GuidGenerator.NewV7();
        }
    }
    public void Created(string createdBy)
    {
        CreatedAt = DateTime.Now;
        CreatedBy = createdBy;
    }

    public void Updated(string updatedBy)
    {
        UpdatedAt = DateTime.Now;
        UpdatedBy = updatedBy;
    }

    public void Deleted(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.Now;
        DeletedBy = deletedBy;
    }
}
