using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Handlers
{
    internal sealed class DelegateSmppSessionPolicy : ISmppSessionPolicy
    {
        private readonly SmppSessionPolicyOptions _options;

        public DelegateSmppSessionPolicy(SmppSessionPolicyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int GetMaxInFlight(SmppSessionContext session)
        {
            return _options.GetMaxInFlight?.Invoke(session)
                   ?? int.MaxValue; // default: unlimited
        }

        public bool AllowMultipleBinds(string systemId)
        {
            return _options.AllowMultipleBinds?.Invoke(systemId)
                   ?? false; // default: single bind only
        }

        public SmppPolicyResult ValidateBind(
            SmppAuthContext authContext,
            SmppSessionContext session)
        {
            return _options.ValidateBind?.Invoke(authContext, session)
                   ?? SmppPolicyResult.Allow();
        }

        public SmppPolicyResult ValidateSubmit(
            SmppSessionContext session,
            SmppSubmitRequest request)
        {
            return _options.ValidateSubmit?.Invoke(session, request)
                   ?? SmppPolicyResult.Allow();
        }
    }



}
