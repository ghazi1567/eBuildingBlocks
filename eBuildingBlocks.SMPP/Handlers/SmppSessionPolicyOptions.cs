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
        /// Validate bind request (credentials, system_type, IP, etc.)
        /// </summary>
        public Func<SmppAuthContext, SmppSessionContext, Task<SmppPolicyResult>>? ValidateBind { get; set; }

        /// <summary>
        /// Validate submit_sm (rate limits, sender rules, content, etc.)
        /// </summary>
        public Func<SmppSessionContext, SmppSubmitRequest, Task<SmppPolicyResult>>? ValidateSubmit { get; set; }

        /// <summary>
        /// Control concurrent in-flight submits per session
        /// </summary>
        public Func<SmppSessionContext, Task<int>>? GetMaxInFlight { get; set; }

        /// <summary>
        /// Allow or deny multiple binds for same system_id
        /// </summary>
        public Func<string, Task<bool>>? AllowMultipleBinds { get; set; }
    }

}
