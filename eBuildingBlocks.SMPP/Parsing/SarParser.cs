using eBuildingBlocks.SMPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class SarParser
    {
        private const ushort TAG_SAR_MSG_REF_NUM = 0x020C;
        private const ushort TAG_SAR_TOTAL_SEGMENTS = 0x020E;
        private const ushort TAG_SAR_SEGMENT_SEQNUM = 0x020F;

        public static SmppConcatInfo? TryParse(Dictionary<ushort, byte[]> tlvs)
        {
            if (!tlvs.TryGetValue(TAG_SAR_MSG_REF_NUM, out var refBytes) ||
                !tlvs.TryGetValue(TAG_SAR_TOTAL_SEGMENTS, out var totalBytes) ||
                !tlvs.TryGetValue(TAG_SAR_SEGMENT_SEQNUM, out var seqBytes))
            {
                return null;
            }

            // ---- sar_msg_ref_num ----
            int refNum;
            if (refBytes.Length == 1)
            {
                refNum = refBytes[0];
            }
            else if (refBytes.Length == 2)
            {
                // SMPP spec: network byte order (big endian)
                refNum = (refBytes[0] << 8) | refBytes[1];
            }
            else
            {
                return null; // invalid TLV length
            }

            // ---- total segments ----
            if (totalBytes.Length != 1 || seqBytes.Length != 1)
                return null;

            int total = totalBytes[0];
            int seq = seqBytes[0];

            // ---- validation ----
            if (total <= 0 || total > 255)
                return null;

            if (seq <= 0 || seq > total)
                return null;

            return new SmppConcatInfo(refNum, total, seq, "SAR");
        }
    }


}
