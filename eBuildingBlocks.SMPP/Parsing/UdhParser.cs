using eBuildingBlocks.SMPP.Models;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class UdhParser
    {
        public static SmppConcatInfo? TryParse(
            byte esmClass,
            ReadOnlySpan<byte> shortMessage,
            out byte[] userDataWithoutUdh)
        {
            userDataWithoutUdh = shortMessage.ToArray();

            if ((esmClass & 0x40) == 0 || shortMessage.Length < 5)
                return null;

            int udhLen = shortMessage[0];
            int udhEnd = 1 + udhLen;

            if (udhLen <= 0 || udhEnd > shortMessage.Length)
                return null;

            // Strip UDH
            userDataWithoutUdh = shortMessage.Slice(udhEnd).ToArray();

            int i = 1;
            while (i + 1 < udhEnd)
            {
                byte iei = shortMessage[i++];
                byte ielen = shortMessage[i++];

                if (i + ielen > udhEnd)
                    break;

                // 8-bit reference
                if (iei == 0x00 && ielen == 0x03)
                {
                    int refNum = shortMessage[i];
                    int total = shortMessage[i + 1];
                    int seq = shortMessage[i + 2];

                    if (total <= 0 || seq <= 0 || seq > total)
                        return null;

                    return new SmppConcatInfo(refNum, total, seq, "UDH-8");
                }

                // 16-bit reference
                if (iei == 0x08 && ielen == 0x04)
                {
                    int refNum = (shortMessage[i] << 8) | shortMessage[i + 1];
                    int total = shortMessage[i + 2];
                    int seq = shortMessage[i + 3];

                    if (total <= 0 || seq <= 0 || seq > total)
                        return null;

                    return new SmppConcatInfo(refNum, total, seq, "UDH-16");
                }

                i += ielen;
            }

            return null;
        }
    }

}
