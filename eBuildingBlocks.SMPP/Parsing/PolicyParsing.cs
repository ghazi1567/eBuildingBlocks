using eBuildingBlocks.SMPP.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class PolicyParsing
    {
        public static SmppBindMode ParseBindModes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SmppBindMode.Transceiver;

            SmppBindMode flags = SmppBindMode.None;
            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<SmppBindMode>(part, ignoreCase: true, out var mode))
                    flags |= mode;
            }
            return flags == SmppBindMode.None ? SmppBindMode.Transceiver : flags;
        }
    }

}
