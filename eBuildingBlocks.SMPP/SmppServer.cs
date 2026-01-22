using eBuildingBlocks.SMPP.Tcp;

namespace eBuildingBlocks.SMPP
{


    public sealed class SmppServer
    {
        private readonly IReadOnlyList<SmppTcpListener> _listeners;

        internal SmppServer(IReadOnlyList<SmppTcpListener> listeners)
        {
            _listeners = listeners;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            Logger.Debug(this.GetType().Name, "StartAsync");
            var tasks = _listeners
                .Select(l => l.StartAsync(ct))
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }

}
