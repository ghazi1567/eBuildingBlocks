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
        BoundTx = 1,
        BoundRx = 2,
        BoundTrx = 3,
        Closed = 4
    }

}
