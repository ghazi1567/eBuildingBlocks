using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class BoundStateSubmitRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            // Session must be bound
            if (session.BindMode == SmppBindMode.None)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVBNDSTS, "Not bound");

            // Receiver cannot submit
            if (session.BindMode == SmppBindMode.Receiver)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVBNDSTS, "Receiver cannot submit");

            if (!policy.IsActive)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVSYSID, "Inactive policy");

            return PolicyDecision.Allow();
        }
    }


    public sealed class RateLimitRule : ISubmitRule
    {
        private readonly IRateLimiter _limiter;
        public RateLimitRule(IRateLimiter limiter) => _limiter = limiter;

        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            if (_limiter.Allow(policy.SystemId, policy.Tps, policy.Mpm))
                return PolicyDecision.Allow();

            return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RTHROTTLED, "Throttled");
        }
    }

    public sealed class TonNpiRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            // Adapt field names to your SmppSubmitRequest model:
            if (policy.AllowedSourceTon.Count > 0 && !policy.AllowedSourceTon.Contains(request.SourceAddrTon))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVSRCADR, "Source TON not allowed");

            if (policy.AllowedSourceNpi.Count > 0 && !policy.AllowedSourceNpi.Contains(request.SourceAddrNpi))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVSRCADR, "Source NPI not allowed");

            if (policy.AllowedDestTon.Count > 0 && !policy.AllowedDestTon.Contains(request.DestAddrTon))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVDSTADR, "Dest TON not allowed");

            if (policy.AllowedDestNpi.Count > 0 && !policy.AllowedDestNpi.Contains(request.DestAddrNpi))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVDSTADR, "Dest NPI not allowed");

            return PolicyDecision.Allow();
        }
    }

    public sealed class AddressPrefixRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            
            if (string.IsNullOrWhiteSpace(request.DestinationAddress))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVDSTADR, "Empty destination address");

            if (string.IsNullOrWhiteSpace(request.SourceAddress))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVSRCADR, "Empty source address");

            if (policy.AllowedSourcePrefixes.Length > 0 &&
                !policy.AllowedSourcePrefixes.Any(p =>
                    request.SourceAddress.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVSRCADR, "Source address not allowed");
            }

            if (policy.AllowedDestPrefixes.Length > 0 &&
                !policy.AllowedDestPrefixes.Any(p =>
                    request.DestinationAddress.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVDSTADR, "Destination address not allowed");
            }

            return PolicyDecision.Allow();
        }
    }


    public sealed class DataCodingRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            if (policy.AllowedDataCodings.Count > 0 && !policy.AllowedDataCodings.Contains(request.DataCoding))
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVCODING, "Data coding not allowed");

            return PolicyDecision.Allow();
        }
    }

    public sealed class ConcatenationRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            if (request.Concat != null && !policy.AllowConcatenation)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVMSGLEN, "Concatenation not allowed");

            return PolicyDecision.Allow();
        }
    }



    public sealed class RegisteredDeliveryRule : ISubmitRule
    {
        public PolicyDecision Evaluate(SmppSessionContext session, SmppSubmitRequest request, SmppAccountPolicy policy)
        {
            bool dlrRequested = request.RegisteredDelivery != 0;

            if (!policy.AllowRegisteredDelivery && dlrRequested)
                return PolicyDecision.Deny((uint)SmppCommandStatus.ESME_RINVREGDLVFLG, "DLR not allowed");

            return PolicyDecision.Allow();
        }
    }

}
