using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Session
{
    public enum SmppSessionState
    {
        Open = 0,
        BoundTrx = 1,
        Closed = 2
    }

}
