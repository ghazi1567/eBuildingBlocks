namespace eBuildingBlocks.SMPP.Models
{
    public sealed class SmppAuthResult
    {

        public bool Success { get; init; }
        public uint CommandStatus { get; init; }

        public SmppAccountPolicy? Policy { get; set; } = new();

        private SmppAuthResult(bool success, uint status, SmppAccountPolicy? policy)
        {
            Success = success;
            CommandStatus = status;
            Policy = policy;
        }

        public static SmppAuthResult Valid() => new(true, 0, null);

        public static SmppAuthResult Valid(SmppAccountPolicy _policy) => new(true, 0, _policy);
        public static SmppAuthResult Fail(uint status) => new(false, status, null);
    }

}
