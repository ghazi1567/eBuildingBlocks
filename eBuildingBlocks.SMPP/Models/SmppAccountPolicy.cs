using eBuildingBlocks.SMPP.Abstractions;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed class SmppAccountPolicy
    {
        public string SystemId { get; init; } = default!;
        public bool IsActive { get; init; }

        // Bind
        public SmppBindMode AllowedBindModes { get; init; } = SmppBindMode.Transceiver;
        public byte MinInterfaceVersion { get; init; } = 0x34; // SMPP 3.4
        public byte? MaxInterfaceVersion { get; init; } = 0x34; // optional
        public string[] AllowedSystemTypes { get; init; } = Array.Empty<string>(); // empty => allow all

        public int MaxBindsPerSystemId { get; init; } = 1;
        public int MaxBindsPerIp { get; init; } = 5;

        // IP allow list (supports exact IP and CIDR)
        public string[] AllowedIpRules { get; init; } = Array.Empty<string>(); // e.g. ["8.213.45.132", "10.0.0.0/24"]

        // Submit limits
        public int MaxInFlightPerSession { get; init; } = 50;

        public int? Tps { get; init; } = 30;   // messages / second
        public int? Mpm { get; init; } = null; // messages / minute

        // TON/NPI allowlists (empty => allow all)
        public HashSet<byte> AllowedSourceTon { get; init; } = new();
        public HashSet<byte> AllowedSourceNpi { get; init; } = new();
        public HashSet<byte> AllowedDestTon { get; init; } = new();
        public HashSet<byte> AllowedDestNpi { get; init; } = new();

        // Address rules (prefix or regex – implement one or both)
        public string[] AllowedSourcePrefixes { get; init; } = Array.Empty<string>(); // e.g. ["BAYT", "INFO"]
        public string[] AllowedDestPrefixes { get; init; } = Array.Empty<string>();   // e.g. ["92", "971"]

        // Coding & length
        public HashSet<byte> AllowedDataCodings { get; init; } = new() { 0, 8 }; // 0=default, 8=UCS2
        public int MaxBodyBytes { get; init; } = 140; // for a single PDU payload without SAR/UDH; tune for your server
        public bool AllowConcatenation { get; init; } = true;
        public bool RequireUdhiForUdhPayload { get; init; } = true;

        // DLR
        public bool AllowRegisteredDelivery { get; init; } = true;


    }
}
