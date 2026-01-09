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

            // service_type (CString)
            _ = SmppPduReader.ReadCString(span, ref o);

            // source_addr_ton, source_addr_npi
            o += 2;
            var sourceAddr = SmppPduReader.ReadCString(span, ref o);

            // dest_addr_ton, dest_addr_npi
            o += 2;
            var destAddr = SmppPduReader.ReadCString(span, ref o);

            // esm_class, protocol_id, priority_flag
            byte esmClass = span[o]; o++;
            o += 2;

            // schedule_delivery_time, validity_period
            _ = SmppPduReader.ReadCString(span, ref o);
            _ = SmppPduReader.ReadCString(span, ref o);

            // registered_delivery, replace_if_present_flag
            o += 2;

            // data_coding
            byte dataCoding = span[o]; o++;

            // sm_default_msg_id
            o += 1;

            // sm_length + short_message
            byte smLen = span[o]; o++;
            if (o + smLen > span.Length) throw new ArgumentException("Invalid short_message length.");

            var shortMsg = span.Slice(o, smLen).ToArray();
            o += smLen;

            // TLVs after short_message
            var tlvs = ParseTlvs(span, o);

            // Concat detection: SAR first, then UDH
            var concat = SarParser.TryParse(tlvs);

            byte[] userData;
            if (concat is null)
            {
                var udh = UdhParser.TryParse(esmClass, shortMsg, out var stripped);
                concat = udh;
                userData = stripped; // without UDH if present
            }
            else
            {
                userData = shortMsg; // SAR typically uses plain user data in short_message
            }

            return new SmppSubmitRequest(
                SourceAddr: sourceAddr,
                DestinationAddr: destAddr,
                DataCoding: dataCoding,
                EsmClass: esmClass,
                UserPayloadBytes: userData,
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
