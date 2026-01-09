using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Handlers
{
    internal sealed class DelegateSmppMessageHandler : ISmppMessageHandler
    {
        private readonly Func<
            SmppSessionContext,
            SmppSubmitRequest,
            CancellationToken,
            Task<SmppSubmitResult>> _handler;

        public DelegateSmppMessageHandler(
            Func<
                SmppSessionContext,
                SmppSubmitRequest,
                CancellationToken,
                Task<SmppSubmitResult>> handler)
        {
            _handler = handler;
        }

        public Task<SmppSubmitResult> HandleSubmitAsync(
            SmppSessionContext session,
            SmppSubmitRequest request,
            CancellationToken ct)
            => _handler(session, request, ct);
    }

}
