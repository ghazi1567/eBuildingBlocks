using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class ActiveAccountBindRule : IBindRule
    {
        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
            => policy.IsActive
                ? PolicyDecision.Allow()
                : PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RBINDFAIL,
                    "Account inactive");
    }


    public sealed class BindModeRule : IBindRule
    {
        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
        {

            if (policy.AllowedBindModes != ctx.RequestedBindMode)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVBNDSTS, "Bind mode not allowed");

            return PolicyDecision.Allow();
        }
    }

    public sealed class InterfaceVersionRule : IBindRule
    {
        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
        {
            if (ctx.InterfaceVersion < policy.MinInterfaceVersion)
                return PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RSYSERR,
                    "Interface version too low");

            if (policy.MaxInterfaceVersion.HasValue &&
                ctx.InterfaceVersion > policy.MaxInterfaceVersion.Value)
                return PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RSYSERR,
                    "Interface version too high");

            return PolicyDecision.Allow();
        }
    }


    public sealed class SystemTypeRule : IBindRule
    {
        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
        {
            if (policy.AllowedSystemTypes == null || policy.AllowedSystemTypes.Length == 0)
                return PolicyDecision.Allow();

            var sysType = (ctx.SystemType ?? string.Empty).Trim();

            if (!policy.AllowedSystemTypes.Any(
                    t => string.Equals(t, sysType, StringComparison.OrdinalIgnoreCase)))
            {
                return PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RINVSYSTYP,
                    "System type not allowed");
            }

            return PolicyDecision.Allow();
        }
    }


    public sealed class IpAllowRule : IBindRule
    {

        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
        {
            if (policy.AllowedIpRules.Length == 0) return PolicyDecision.Allow();

            var ip = ctx.RemoteIp;

            if (policy.AllowedIpRules.Any(r => r.Contains(ip.ToString())))
                return PolicyDecision.Allow();

            // Better than RSYSERR
            return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RBINDFAIL, "IP not allowed");
        }
    }


    public sealed class DuplicateBindRule : IBindRule
    {
        public PolicyDecision Evaluate(SmppAuthContext ctx, SmppSessionContext session, SmppAccountPolicy policy)
        {
            if (session.BindMode != SmppBindMode.None)
                return PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RALYBND,
                    "Already bound");

            return PolicyDecision.Allow();
        }
    }

    public sealed class MaxBindRule : IBindRule
    {
        private readonly IBindRegistry _registry;

        public MaxBindRule(IBindRegistry registry)
        {
            _registry = registry;
        }

        public PolicyDecision Evaluate(
            SmppAuthContext ctx,
            SmppSessionContext session,
            SmppAccountPolicy policy)
        {
            if (policy.MaxBindsPerIp <= 0)
                return PolicyDecision.Allow();

            var current = _registry.GetBindCount(ctx.SystemId);

            if (current >= policy.MaxBindsPerSystemId)
            {
                return PolicyDecision.Deny(
                    (uint)SmppCommandStatus.ESME_RALYBND,
                    "Maximum bind limit reached");
            }

            return PolicyDecision.Allow();
        }
    }

}
