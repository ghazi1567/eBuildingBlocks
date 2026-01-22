using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface IBindRule
    {
        PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy);
    }
}
