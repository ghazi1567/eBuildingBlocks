using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppSubmitRequest(
        string SourceAddr,
        string DestinationAddr,
        byte DataCoding,
        byte EsmClass,
        byte[] UserPayloadBytes,
        SmppConcatInfo? Concat
    );

}
