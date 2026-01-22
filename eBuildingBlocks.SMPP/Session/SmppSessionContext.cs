using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using System.Net;

namespace eBuildingBlocks.SMPP.Session
{
    public sealed class SmppSessionContext
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public SmppSessionState State { get; set; } = SmppSessionState.Open;
        public string? SystemId { get; set; }

        public int InFlightSubmits;
        public byte AddrTon; 
        public byte AddrNpi;
        public string? AddressRange;
        public IPAddress? RemoteIp;
        public int LocalPort;
        public SmppBindMode BindMode;
        public byte InterfaceVersion;
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
        public SmppAccountPolicy? Policy { get; set; }

    }


}
