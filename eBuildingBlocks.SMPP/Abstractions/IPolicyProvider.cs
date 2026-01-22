using eBuildingBlocks.SMPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface IPolicyProvider
    {
        Task<SmppAccountPolicy?> GetPolicyAsync(string systemId);
    }
}
