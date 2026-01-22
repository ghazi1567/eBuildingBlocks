using eBuildingBlocks.SMPP.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class ExactIpRule : IIpRule
    {
        private readonly IPAddress _ip;
        public ExactIpRule(IPAddress ip) => _ip = ip;
        public bool Matches(IPAddress ip) => ip.Equals(_ip);
    }
}
