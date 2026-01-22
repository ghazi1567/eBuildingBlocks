using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppSubmitRequest(
        byte SourceAddrTon,
        byte SourceAddrNpi,
        string SourceAddress,

        byte DestAddrTon,
        byte DestAddrNpi,
        string DestinationAddress,

        byte DataCoding,
        byte EsmClass,
        byte RegisteredDelivery,

        ReadOnlyMemory<byte> UserPayloadBytes,
        SmppConcatInfo? Concat

    );

}
