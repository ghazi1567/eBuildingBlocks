using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Session;
using System.Collections.Concurrent;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class InMemoryBindRegistry : IBindRegistry
    {
        // systemId → active bind count
        private readonly ConcurrentDictionary<string, int> _binds = new();
        private readonly ConcurrentDictionary<string, SmppSessionContext> _sessions = new();

        public IEnumerable<KeyValuePair<string, SmppSessionContext>> Sessions => _sessions;
        public bool TryAdd(string systemId, SmppSessionContext session) => _sessions.TryAdd(systemId, session);
        public bool TryRemove(string systemId, out SmppSessionContext session) => _sessions.TryRemove(systemId, out session);

        public bool TryGet(string systemId, out SmppSessionContext? session)
        {
            return _sessions.TryGetValue(systemId, out session);
        }

        /// <summary>
        /// Convenience method if you only need the session objects.
        /// </summary>
        public IEnumerable<SmppSessionContext> GetAll() => _sessions.Values;


        public void Register(SmppSessionContext session)
        {
            if (string.IsNullOrEmpty(session.SystemId))
                return;

            _binds.AddOrUpdate(
                session.SystemId,
                1,
                (_, count) => count + 1);

            TryAdd(session.SystemId, session);
        }

        public void Unregister(SmppSessionContext session)
        {
            if (string.IsNullOrEmpty(session.SystemId))
                return;

            _binds.AddOrUpdate(
                session.SystemId,
                0,
                (_, count) => Math.Max(0, count - 1));

            TryRemove(session.SystemId, out _);

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
