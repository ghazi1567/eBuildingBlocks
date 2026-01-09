using eBuildingBlocks.SMPP.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Tcp
{
    public sealed class SmppFrameDecoder
    {
        private readonly MemoryStream _buf = new();

        public void Append(ReadOnlySpan<byte> data) => _buf.Write(data);

        public bool TryReadFrame(out byte[] frame)
        {
            frame = Array.Empty<byte>();
            if (_buf.Length < 4) return false;

            var all = _buf.ToArray();
            uint len = SmppPduReader.ReadU32(all.AsSpan(0, 4));

            // sanity
            if (len < 16 || len > 1024 * 1024) throw new InvalidOperationException($"Bad PDU length: {len}");

            if (_buf.Length < len) return false;

            frame = all.AsSpan(0, (int)len).ToArray();
            var remaining = all.AsSpan((int)len).ToArray();

            _buf.SetLength(0);
            _buf.Write(remaining);

            return true;
        }
    }

}
