using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppSubmitResult(string MessageId, SmppCommandStatus Status);

}
