using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Interfaces
{
    /// <summary>
    /// Marker interface: identifies entities that are aggregate roots.
    /// Repositories should operate only on aggregate roots.
    /// </summary>
    public interface IAggregateRoot { }
}
