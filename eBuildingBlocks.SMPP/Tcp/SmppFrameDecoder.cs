using eBuildingBlocks.SMPP.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
namespace eBuildingBlocks.SMPP.Tcp;
public sealed class SmppFrameDecoder
{
    private byte[] _buffer = new byte[64 * 1024];
    private int _length = 0;

    public void Append(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(_length + data.Length);
        data.CopyTo(_buffer.AsSpan(_length));
        _length += data.Length;
    }

    public bool TryReadFrame(out byte[] frame)
    {
        frame = null;

        if (_length < 4)
            return false;

        uint pduLength = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(0, 4));

        // sanity check (DO NOT THROW)
        if (pduLength < 16 || pduLength > 1024 * 1024)
            throw new InvalidOperationException($"Invalid SMPP PDU length: {pduLength}");

        if (_length < pduLength)
            return false;

        frame = new byte[pduLength];
        Array.Copy(_buffer, 0, frame, 0, (int)pduLength);

        // shift remaining bytes
        int remaining = _length - (int)pduLength;
        if (remaining > 0)
            Array.Copy(_buffer, (int)pduLength, _buffer, 0, remaining);

        _length = remaining;
        return true;
    }

    private void EnsureCapacity(int size)
    {
        if (_buffer.Length >= size)
            return;

        int newSize = Math.Max(size, _buffer.Length * 2);
        Array.Resize(ref _buffer, newSize);
    }
}

