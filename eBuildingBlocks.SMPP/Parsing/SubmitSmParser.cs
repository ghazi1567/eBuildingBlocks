using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Protocol;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class SubmitSmParser
    {
        public static SmppSubmitRequest Parse(byte[] pdu)
        {
            var span = (ReadOnlySpan<byte>)pdu;
            int o = 16;

            _ = SmppPduReader.ReadCString(span, ref o); // service_type

            o += 2;
            var sourceAddr = SmppPduReader.ReadCString(span, ref o);

            o += 2;
            var destAddr = SmppPduReader.ReadCString(span, ref o);

            byte esmClass = span[o]; o++;
            o += 2;

            _ = SmppPduReader.ReadCString(span, ref o);
            _ = SmppPduReader.ReadCString(span, ref o);

            o += 2;

            byte dataCoding = span[o]; o++;
            o += 1;

            byte smLen = span[o]; o++;
            if (o + smLen > span.Length)
                throw new ArgumentException("Invalid short_message length.");

            var shortMsg = span.Slice(o, smLen).ToArray();
            o += smLen;

            var tlvs = ParseTlvs(span, o);

            // STEP 1: Pick correct user data source
            byte[] payload =
                tlvs.TryGetValue(0x0424, out var messagePayload)
                    ? messagePayload
                    : shortMsg;

            // STEP 2: Detect concatenation (SAR first)
            var concat = SarParser.TryParse(tlvs);

            // STEP 3: Strip UDH if present
            if (concat is null)
            {
                var udh = UdhParser.TryParse(esmClass, payload, out var stripped);
                concat = udh;
                payload = stripped;
            }

            return new SmppSubmitRequest(
                SourceAddr: sourceAddr,
                DestinationAddr: destAddr,
                DataCoding: dataCoding,
                EsmClass: esmClass,
                UserPayloadBytes: payload,
                Concat: concat
            );
        }


        private static Dictionary<ushort, byte[]> ParseTlvs(ReadOnlySpan<byte> span, int offset)
        {
            var tlvs = new Dictionary<ushort, byte[]>();
            int o = offset;

            while (o + 4 <= span.Length)
            {
                ushort tag = SmppPduReader.ReadU16(span.Slice(o, 2));
                ushort len = SmppPduReader.ReadU16(span.Slice(o + 2, 2));
                o += 4;

                if (o + len > span.Length) break;

                tlvs[tag] = span.Slice(o, len).ToArray();
                o += len;
            }

            return tlvs;
        }
    }

}
