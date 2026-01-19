using eBuildingBlocks.SMPP.Abstractions;
using System.Net;

namespace eBuildingBlocks.SMPP.Session
{
    public sealed class SmppSessionContext
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public SmppSessionState State { get; set; } = SmppSessionState.Open;
        public string? SystemId { get; set; }

        public int InFlightSubmits; // updated atomically
        public byte AddrTon; // updated atomically
        public IPAddress RemoteIp;
        public int LocalPort;
        public SmppBindMode BindMode;
        public byte InterfaceVersion;

    }


}
