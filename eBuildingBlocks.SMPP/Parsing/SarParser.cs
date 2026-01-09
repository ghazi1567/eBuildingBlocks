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
        // SMPP SAR TLVs
        private const ushort TAG_SAR_MSG_REF_NUM = 0x020C; // 2 bytes (often)
        private const ushort TAG_SAR_TOTAL_SEGMENTS = 0x020E; // 1 byte
        private const ushort TAG_SAR_SEGMENT_SEQNUM = 0x020F; // 1 byte

        public static SmppConcatInfo? TryParse(Dictionary<ushort, byte[]> tlvs)
        {
            if (!tlvs.TryGetValue(TAG_SAR_MSG_REF_NUM, out var r) ||
                !tlvs.TryGetValue(TAG_SAR_TOTAL_SEGMENTS, out var t) ||
                !tlvs.TryGetValue(TAG_SAR_SEGMENT_SEQNUM, out var s))
            {
                return null;
            }

            int refNum = r.Length >= 2 ? (r[0] << 8 | r[1]) : r[0];
            int total = t[0];
            int seq = s[0];

            if (total <= 0 || seq <= 0) return null;

            return new SmppConcatInfo(refNum, total, seq, "SAR");
        }
    }

}
