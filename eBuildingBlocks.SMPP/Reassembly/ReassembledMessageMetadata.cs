using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Reassembly
{
    public sealed record ReassembledMessageMetadata
    {
        public string SystemId { get; init; } = null!;
        public Guid SessionId { get; init; }
        public DateTime ReceivedAtUtc { get; init; }

        // Optional network info
        public string? ClientIp { get; init; }
        public int? ClientPort { get; init; }

        // Optional SMPP info
        public byte? EsmClass { get; init; }
        public int? ReferenceNumber { get; init; }
        public int? TotalParts { get; init; }
    }

}
