using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISubmitRule
    {
        PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy);
    }
}
