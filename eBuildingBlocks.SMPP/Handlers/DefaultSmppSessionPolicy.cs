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
    public sealed class DefaultSmppSessionPolicy : ISmppSessionPolicy
    {
        private readonly IEnumerable<ISubmitRule> _submitRules;
        private readonly IEnumerable<IBindRule> _bindRules;

        public DefaultSmppSessionPolicy(
            IEnumerable<ISubmitRule> submitRules,
            IEnumerable<IBindRule> bindRules)
        {
            _submitRules = submitRules;
            _bindRules = bindRules;
        }

        public Task<SmppPolicyResult> ValidateSubmit(
            SmppSessionContext session,
            SmppSubmitRequest request)
        {
            var policy = session.Policy;

            Logger.Debug(this.GetType().Name, $"policy is null : {policy is null}");

            if (policy == null)
                return Task.FromResult(
                    SmppPolicyResult.Allow());

            foreach (var rule in _submitRules)
            {
                var decision = rule.Evaluate(session, request, policy);
                if (!decision.Allowed)
                    return Task.FromResult(SmppPolicyResult.Deny(decision.Status));
            }

            return Task.FromResult(SmppPolicyResult.Allow());
        }

        public Task<SmppPolicyResult> ValidateBind(
            SmppAuthContext authContext,
            SmppSessionContext session)
        {
            Logger.Debug(this.GetType().Name, $"ValidateBind : ");
            foreach (var rule in _bindRules)
            {
                var decision = rule.Evaluate(authContext, session, session.Policy);
                if (!decision.Allowed)
                    return Task.FromResult(SmppPolicyResult.Deny(decision.Status));
            }

            return Task.FromResult(SmppPolicyResult.Allow());
        }

        public Task<bool> AllowMultipleBinds(string systemId)
        {
            // Demo behavior: only one bind per systemId
            return Task.FromResult(false);
        }

        public Task<int> GetMaxInFlight(SmppSessionContext session)
        {
            return Task.FromResult(
                session.Policy?.MaxInFlightPerSession ?? 50);
        }
    }
}
