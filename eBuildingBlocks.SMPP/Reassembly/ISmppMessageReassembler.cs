using eBuildingBlocks.SMPP.Models;

namespace eBuildingBlocks.SMPP.Reassembly
{

    /// <summary>
    /// Optional message reassembly contract.
    /// Implementations may use memory, DB, Redis, etc.
    /// </summary>
    public interface ISmppMessageReassembler
    {
        /// <summary>
        /// Adds a message part and returns a fully reassembled message
        /// when all parts are received. Returns null otherwise.
        /// </summary>
        ReassembledMessage? TryAddPart(
            SmppSessionContext session,
            SmppSubmitRequest request);
    }

}
