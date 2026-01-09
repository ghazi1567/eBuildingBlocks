using eBuildingBlocks.SMPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Session
{
    public sealed class SmppSession
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public SmppSessionState State { get; set; } = SmppSessionState.Open;
        public string? SystemId { get; set; }

        public int InFlightSubmits; // updated atomically
    }


}
