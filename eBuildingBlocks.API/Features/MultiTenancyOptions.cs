using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.API.Features
{
    public sealed class MultiTenancyOptions
    {
        public bool Enabled { get; set; } = false;
        public string HeaderName { get; set; } = "X-TenantId";
        public Guid DefaultTenantId { get; set; }
    }
}
