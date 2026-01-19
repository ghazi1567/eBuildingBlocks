using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed class SmppPolicyResult
    {
        public bool Allowed { get; init; }
        public uint CommandStatus { get; init; }

        public static SmppPolicyResult Allow() =>
            new() { Allowed = true, CommandStatus = (uint)SmppCommandStatus.ESME_ROK };

        public static SmppPolicyResult Deny(uint status) =>
            new() { Allowed = false, CommandStatus = status };
    }

}
