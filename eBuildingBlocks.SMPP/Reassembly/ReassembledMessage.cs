namespace eBuildingBlocks.SMPP.Reassembly
{

    /// <summary>
    /// Represents a fully reassembled SMS message.
    /// </summary>
    public sealed record ReassembledMessage(
        ReassembledMessageMetadata Metadata,
        string SourceAddr,
        string DestinationAddr,
        byte DataCoding,
        byte[] FullPayload,
        string? Text
    );
    
}
