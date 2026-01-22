using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface IRateLimiter
    {
        bool Allow(string key, int? tps, int? mpm);
    }
}
