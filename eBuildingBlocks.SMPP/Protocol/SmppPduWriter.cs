using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Protocol
{
    public static class SmppPduWriter
    {
        public static byte[] BuildResponse(uint commandId, uint status, uint sequence, ReadOnlySpan<byte> body)
        {
            uint len = (uint)(16 + body.Length);
            var resp = new byte[len];

            WriteU32(resp.AsSpan(0, 4), len);
            WriteU32(resp.AsSpan(4, 4), commandId);
            WriteU32(resp.AsSpan(8, 4), status);
            WriteU32(resp.AsSpan(12, 4), sequence);

            body.CopyTo(resp.AsSpan(16));
            return resp;
        }

        public static byte[] CString(string s) => Encoding.ASCII.GetBytes(s + "\0");

        public static void WriteU32(Span<byte> s, uint v)
        {
            s[0] = (byte)(v >> 24);
            s[1] = (byte)(v >> 16);
            s[2] = (byte)(v >> 8);
            s[3] = (byte)v;
        }
    }

}
