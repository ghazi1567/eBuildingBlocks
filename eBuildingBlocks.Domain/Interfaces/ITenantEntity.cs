using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Interfaces
{
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
