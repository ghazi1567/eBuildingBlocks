using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Session;
using System.Net;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppAuthContext(
        string SystemId,
        string Password,
        SmppBindMode RequestedBindMode,
        string? SystemType,
        byte InterfaceVersion,
        IPAddress RemoteIp,
        int LocalPort,
        SmppSessionContext Session
     );

}
