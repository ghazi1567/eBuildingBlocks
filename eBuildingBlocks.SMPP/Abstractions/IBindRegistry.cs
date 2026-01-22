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

        IEnumerable<KeyValuePair<string, SmppSessionContext>> Sessions { get; }

        bool TryAdd(string systemId, SmppSessionContext session);
        bool TryRemove(string systemId, out SmppSessionContext? session);
        bool TryGet(string systemId, out SmppSessionContext? session);

        IEnumerable<SmppSessionContext> GetAll();
    }

}
