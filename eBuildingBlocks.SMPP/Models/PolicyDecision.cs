namespace eBuildingBlocks.SMPP.Models
{
    public sealed record PolicyDecision(bool Allowed, uint Status, string? Reason = null)
    {
        public static PolicyDecision Allow() => new(true, 0);
        public static PolicyDecision Deny(uint status, string? reason = null) => new(false, status, reason);
    }

}
