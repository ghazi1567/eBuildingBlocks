using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface IBindRegistry
    {
        void Register(SmppSessionContext session);
        void Unregister(SmppSessionContext session);
        int GetBindCount(string systemId);
    }

}
