using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppAuthContext(
         string SystemId,
         string Password,
         SmppSessionContext Session
     );

}
