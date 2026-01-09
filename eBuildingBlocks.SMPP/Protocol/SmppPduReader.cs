using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Protocol
{
    public static class SmppPduReader
    {
        public static SmppHeader ReadHeader(ReadOnlySpan<byte> pdu)
        {
            if (pdu.Length < 16) throw new ArgumentException("PDU too short.");
            return new SmppHeader(
                Length: ReadU32(pdu.Slice(0, 4)),
                CommandId: ReadU32(pdu.Slice(4, 4)),
                Status: ReadU32(pdu.Slice(8, 4)),
                Sequence: ReadU32(pdu.Slice(12, 4))
            );
        }

        public static string ReadCString(ReadOnlySpan<byte> pdu, ref int offset)
        {
            int start = offset;
            while (offset < pdu.Length && pdu[offset] != 0) offset++;
            if (offset >= pdu.Length) throw new ArgumentException("Invalid C-string (no terminator).");

            var s = Encoding.ASCII.GetString(pdu.Slice(start, offset - start));
            offset++; // skip null
            return s;
        }

        public static uint ReadU32(ReadOnlySpan<byte> s)
            => (uint)(s[0] << 24 | s[1] << 16 | s[2] << 8 | s[3]);

        public static ushort ReadU16(ReadOnlySpan<byte> s)
            => (ushort)((s[0] << 8) | s[1]);
    }

}
