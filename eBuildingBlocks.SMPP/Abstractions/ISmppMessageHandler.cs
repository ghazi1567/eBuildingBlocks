using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISmppMessageHandler
    {
        Task<SmppSubmitResult> HandleSubmitAsync(
            SmppSessionContext session,
            SmppSubmitRequest request,
            CancellationToken ct);
    }

}
