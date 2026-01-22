namespace eBuildingBlocks.SMPP
{
    public static class Logger
    {
        public static void Debug(string stage,byte[] payload,byte dataCoding,byte esmClass)
        {
            Console.WriteLine("=================================");
            Console.WriteLine($"[{stage}]");
            Console.WriteLine($"data_coding : 0x{dataCoding:X2}");
            Console.WriteLine($"esm_class   : 0x{esmClass:X2}");
            Console.WriteLine($"payload_len : {payload.Length}");
            Console.WriteLine("HEX         : " + BitConverter.ToString(payload));
        }
        public static void Debug(string stage, string message)
        {
            Console.WriteLine("=================================");
            Console.WriteLine($"[{stage}]");
            Console.WriteLine($"message : {message}");
            Console.WriteLine("=================================");
        }
    }
}
