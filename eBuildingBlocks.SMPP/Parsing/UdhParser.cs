using eBuildingBlocks.SMPP.Models;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class UdhParser
    {
        public static SmppConcatInfo? TryParse(byte esmClass, ReadOnlySpan<byte> shortMessage, out byte[] userDataWithoutUdh)
        {
            userDataWithoutUdh = shortMessage.ToArray();

            // UDHI flag is 0x40
            bool udhi = (esmClass & 0x40) != 0;
            if (!udhi || shortMessage.Length < 6) return null;

            int udhLen = shortMessage[0];
            if (udhLen <= 0) return null;
            if (1 + udhLen > shortMessage.Length) return null;

            // Strip UDH: [UDHL][UDH bytes...][User data...]
            userDataWithoutUdh = shortMessage.Slice(1 + udhLen).ToArray();

            int i = 1;
            int end = 1 + udhLen;

            while (i + 1 < end)
            {
                byte iei = shortMessage[i++];
                byte ielen = shortMessage[i++];
                if (i + ielen > shortMessage.Length) break;

                // 8-bit ref: IEI 0x00 length 0x03 [ref][total][seq]
                if (iei == 0x00 && ielen == 0x03)
                {
                    int refNum = shortMessage[i];
                    int total = shortMessage[i + 1];
                    int seq = shortMessage[i + 2];
                    return new SmppConcatInfo(refNum, total, seq, "UDH-8");
                }

                // 16-bit ref: IEI 0x08 length 0x04 [refMSB][refLSB][total][seq]
                if (iei == 0x08 && ielen == 0x04)
                {
                    int refNum = (shortMessage[i] << 8) | shortMessage[i + 1];
                    int total = shortMessage[i + 2];
                    int seq = shortMessage[i + 3];
                    return new SmppConcatInfo(refNum, total, seq, "UDH-16");
                }

                i += ielen;
            }

            return null;
        }
    }

}
