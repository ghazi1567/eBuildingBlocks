using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class InMemoryBindRegistry : IBindRegistry
    {
        // systemId → active bind count
        private readonly ConcurrentDictionary<string, int> _binds = new();

        public void Register(SmppSessionContext session)
        {
            if (string.IsNullOrEmpty(session.SystemId))
                return;

            _binds.AddOrUpdate(
                session.SystemId,
                1,
                (_, count) => count + 1);
        }

        public void Unregister(SmppSessionContext session)
        {
            if (string.IsNullOrEmpty(session.SystemId))
                return;

            _binds.AddOrUpdate(
                session.SystemId,
                0,
                (_, count) => Math.Max(0, count - 1));

            // Cleanup zero entries (important)
            if (_binds.TryGetValue(session.SystemId, out var count) && count == 0)
                _binds.TryRemove(session.SystemId, out _);
        }

        public int GetBindCount(string systemId)
        {
            if (string.IsNullOrEmpty(systemId))
                return 0;

            return _binds.TryGetValue(systemId, out var count)
                ? count
                : 0;
        }
    }
}
