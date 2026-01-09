using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class SmppSessionPolicyOptions
    {
        /// <summary>
        /// Called when a client attempts to bind.
        /// Default: allow all.
        /// </summary>
        public Func<string, bool>? CanBind { get; set; }

        /// <summary>
        /// Called for every submit_sm.
        /// Default: allow all.
        /// </summary>
        public Func<SmppSessionContext, bool>? CanSubmit { get; set; }

        /// <summary>
        /// Max concurrent in-flight submits per session.
        /// Default: unlimited.
        /// </summary>
        public int? MaxInFlightPerSession { get; set; }
    }

}
