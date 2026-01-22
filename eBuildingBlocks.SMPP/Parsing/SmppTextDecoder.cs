using System.Text;

namespace eBuildingBlocks.SMPP.Parsing
{
    internal static class SmppTextDecoder
    {
        //  NEW: Preferred overload
        public static string? TryDecode(ReadOnlyMemory<byte> payload, byte dataCoding)
        {
            return TryDecode(payload.Span, dataCoding);
        }

        // Existing overload (kept for compatibility)
        public static string? TryDecode(byte[] payload, byte dataCoding)
        {
            if (payload == null)
                return string.Empty;

            return TryDecode(payload.AsSpan(), dataCoding);
        }

        //  Single implementation (no duplication)
        private static string? TryDecode(ReadOnlySpan<byte> payload, byte dataCoding)
        {
            try
            {
                if (payload.Length == 0)
                    return string.Empty;

                return dataCoding switch
                {
                    0x00 => DecodeGsm7(payload),                          // GSM default alphabet
                    0x03 => Encoding.Latin1.GetString(payload),           // ISO-8859-1
                    0x08 => Encoding.BigEndianUnicode.GetString(payload), // UCS-2
                    _ => DecodeWithFallback(payload)
                };
            }
            catch
            {
                return null;
            }
        }

        private static string DecodeWithFallback(ReadOnlySpan<byte> payload)
        {
            try
            {
                return Encoding.BigEndianUnicode.GetString(payload);
            }
            catch
            {
                return DecodeGsm7(payload);
            }
        }

        // -------------------------------------------------------
        // GSM-7 DECODER (ESC + extension table)
        // -------------------------------------------------------
        private static string DecodeGsm7(ReadOnlySpan<byte> data)
        {
            var sb = new StringBuilder(data.Length);
            bool escape = false;

            foreach (byte b in data)
            {
                if (escape)
                {
                    sb.Append(Gsm7ExtensionTable.TryGetValue(b, out var ch) ? ch : '?');
                    escape = false;
                    continue;
                }

                if (b == 0x1B)
                {
                    escape = true;
                    continue;
                }

                sb.Append(Gsm7BasicTable[b & 0x7F]);
            }

            return sb.ToString();
        }

        // GSM-7 BASIC CHARACTER TABLE
        private static readonly char[] Gsm7BasicTable =
        {
            '@','£','$','¥','è','é','ù','ì','ò','Ç','\n','Ø','ø','\r','Å','å',
            'Δ','_','Φ','Γ','Λ','Ω','Π','Ψ','Σ','Θ','Ξ','\u001B','Æ','æ','ß','É',
            ' ','!','"','#','¤','%','&','\'','(',')','*','+',
            ',', '-', '.', '/',
            '0','1','2','3','4','5','6','7','8','9',
            ':',';','<','=','>','?',
            '¡','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z',
            'Ä','Ö','Ñ','Ü','§',
            '¿','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z',
            'ä','ö','ñ','ü','à'
        };

        // GSM-7 EXTENSION TABLE (ESC 0x1B)
        private static readonly Dictionary<byte, char> Gsm7ExtensionTable = new()
        {
            { 0x0A, '\f' },
            { 0x14, '^' },
            { 0x28, '{' },
            { 0x29, '}' },
            { 0x2F, '\\' },
            { 0x3C, '[' },
            { 0x3D, '~' },
            { 0x3E, ']' },
            { 0x40, '|' },
            { 0x65, '€' }
        };
    }
}
