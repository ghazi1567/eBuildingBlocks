using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Parsing
{
    internal static class SmppTextDecoder
    {
        public static string? TryDecode(byte[] payload, byte dataCoding)
        {
            try
            {
                return dataCoding switch
                {
                    0 => Encoding.ASCII.GetString(payload),                 // GSM 7-bit (basic)
                    3 => Encoding.Latin1.GetString(payload),               // Latin-1
                    8 => Encoding.BigEndianUnicode.GetString(payload),     // UCS2
                    _ => null                                               // Unknown / binary
                };
            }
            catch
            {
                return null;
            }
        }
    }

}
