using eBuildingBlocks.SMPP.Abstractions;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed class SmppAuthResult
    {
        public bool Success { get; init; }
        public uint CommandStatus { get; init; }
        public SmppBindMode RequestedBindMode { get; init; }
        public string? SystemType { get; init; }
        public byte InterfaceVersion { get; init; }
        public byte AddrTon { get; init; }
        public string? AllowedIPAddress { get; init; }
        public int LocalPort { get; init; }

        public static SmppAuthResult Valid() =>
            new() { Success = true, CommandStatus = (uint)SmppCommandStatus.ESME_ROK };
        public static SmppAuthResult Fail(uint status) =>
            new() { Success = false, CommandStatus = status };
    }

}
