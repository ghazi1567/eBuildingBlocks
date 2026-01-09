namespace eBuildingBlocks.SMPP.Protocol
{
    public readonly record struct SmppHeader(uint Length, uint CommandId, uint Status, uint Sequence);

    public static class Be
    {
        public static uint ReadU32(ReadOnlySpan<byte> s)
            => (uint)(s[0] << 24 | s[1] << 16 | s[2] << 8 | s[3]);

        public static void WriteU32(Span<byte> s, uint v)
        {
            s[0] = (byte)(v >> 24); s[1] = (byte)(v >> 16); s[2] = (byte)(v >> 8); s[3] = (byte)v;
        }
    }

   
}
