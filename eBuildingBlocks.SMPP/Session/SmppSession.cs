namespace eBuildingBlocks.SMPP.Session
{
    public sealed class SmppSessionContext
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public SmppSessionState State { get; set; } = SmppSessionState.Open;
        public string? SystemId { get; set; }

        public int InFlightSubmits; // updated atomically
    }


}
