using System;

namespace eBuildingBlocks.Domain.Interfaces
{
    /// <summary>
    /// Non-generic marker for audit-stamped entities, independent of the primary key type.
    /// Lets infrastructure code (e.g. the audit <c>SaveChangesInterceptor</c>) query
    /// <c>ChangeTracker.Entries&lt;IAuditableEntity&gt;()</c> regardless of whether the entity's
    /// key is <see cref="Guid"/>, <see cref="int"/>, <see cref="long"/>, or <see cref="string"/> —
    /// querying <c>Entries&lt;AuditableEntity&lt;Guid&gt;&gt;()</c> directly silently skips every
    /// other key type.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>UTC timestamp when the entity was created.</summary>
        DateTimeOffset CreatedOn { get; }

        /// <summary>Identifier of the creator (user id/email/service), if available.</summary>
        string? CreatedBy { get; }

        /// <summary>UTC timestamp when the entity was last modified.</summary>
        DateTimeOffset? ModifiedOn { get; }

        /// <summary>Identifier of the last modifier, if available.</summary>
        string? ModifiedBy { get; }

        /// <summary>Helper to set creation audit. Call from your context when state is Added.</summary>
        void SetCreated(string? userId, DateTimeOffset? when = null);

        /// <summary>Helper to set modification audit. Call from your context when state is Modified.</summary>
        void SetModified(string? userId, DateTimeOffset? when = null);
    }
}
