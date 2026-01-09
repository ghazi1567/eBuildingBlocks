using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    public sealed record SmppConcatInfo(int Ref, int Total, int Seq, string Method);

}
