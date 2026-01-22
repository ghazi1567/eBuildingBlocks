using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Protocol;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class SubmitSmParser
    {
        public static SmppSubmitRequest Parse(byte[] pdu)
        {
            var span = (ReadOnlySpan<byte>)pdu;
            int o = 16; // Skip SMPP header

            // service_type
            _ = SmppPduReader.ReadCString(span, ref o);

            // source_addr_ton / npi
            byte sourceTon = span[o++];
            byte sourceNpi = span[o++];

            // source_addr
            var sourceAddr = SmppPduReader.ReadCString(span, ref o);

            // dest_addr_ton / npi
            byte destTon = span[o++];
            byte destNpi = span[o++];

            // destination_addr
            var destAddr = SmppPduReader.ReadCString(span, ref o);

            // esm_class
            byte esmClass = span[o++];

            // protocol_id (ignored)
            o++;

            // priority_flag (ignored)
            o++;

            // schedule_delivery_time
            _ = SmppPduReader.ReadCString(span, ref o);

            // validity_period
            _ = SmppPduReader.ReadCString(span, ref o);

            // registered_delivery
            byte registeredDelivery = span[o++];

            // replace_if_present_flag (ignored)
            o++;

            // data_coding
            byte dataCoding = span[o++];

            // sm_default_msg_id (ignored)
            o++;

            // sm_length
            byte smLen = span[o++];

            if (o + smLen > span.Length)
                throw new ArgumentException("Invalid short_message length.");

            // short_message
            var shortMsg = span.Slice(o, smLen).ToArray();
            o += smLen;

            // TLVs
            var tlvs = ParseTlvs(span, o);

            // STEP 1: Pick correct user data source
            byte[] payload =
                tlvs.TryGetValue(0x0424, out var messagePayload)
                    ? messagePayload
                    : shortMsg;

            // STEP 2: Detect concatenation (SAR TLVs first)
            var concat = SarParser.TryParse(tlvs);

            // STEP 3: Strip UDH if present
            if (concat is null)
            {
                var udh = UdhParser.TryParse(esmClass, payload, out var stripped);
                concat = udh;
                payload = stripped;
            }

            return new SmppSubmitRequest(
                SourceAddrTon: sourceTon,
                SourceAddrNpi: sourceNpi,
                SourceAddress: sourceAddr,

                DestAddrTon: destTon,
                DestAddrNpi: destNpi,
                DestinationAddress: destAddr,

                DataCoding: dataCoding,
                EsmClass: esmClass,
                RegisteredDelivery: registeredDelivery,

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

                if (o + len > span.Length)
                    break;

                tlvs[tag] = span.Slice(o, len).ToArray();
                o += len;
            }

            return tlvs;
        }
    }
}
