using eBuildingBlocks.SMPP.Models;

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
