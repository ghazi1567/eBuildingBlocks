using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Models
{
    /// <summary>
    /// Adds audit fields on top of <see cref="BaseEntity{TKey}"/>.
    /// Stamp these in your DbContext (CreatedOn/By on Add; ModifiedOn/By on Update).
    /// </summary>
    public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>UTC timestamp when the entity was created.</summary>
        public DateTimeOffset CreatedOn { get; protected set; }

        /// <summary>Identifier of the creator (user id/email/service), if available.</summary>
        public string? CreatedBy { get; protected set; }

        /// <summary>UTC timestamp when the entity was last modified.</summary>
        public DateTimeOffset? ModifiedOn { get; protected set; }

        /// <summary>Identifier of the last modifier, if available.</summary>
        public string? ModifiedBy { get; protected set; }

        /// <summary>
        /// Helper to set creation audit. Call from your context when state is Added.
        /// </summary>
        public void SetCreated(string? userId, DateTimeOffset? when = null)
        {
            CreatedOn = when ?? DateTimeOffset.UtcNow;
            CreatedBy = userId;
        }

        /// <summary>
        /// Helper to set modification audit. Call from your context when state is Modified.
        /// </summary>
        public void SetModified(string? userId, DateTimeOffset? when = null)
        {
            ModifiedOn = when ?? DateTimeOffset.UtcNow;
            ModifiedBy = userId;
        }
    }
}
