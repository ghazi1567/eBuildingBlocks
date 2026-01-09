using eBuildingBlocks.SMPP.Models;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISmppAuthenticator
    {
        //Task<bool> AuthenticateAsync(string systemId, string password, CancellationToken ct);

        Task<bool> AuthenticateAsync(SmppAuthContext context);

    }

}
