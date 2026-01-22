using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Handlers
{
    internal sealed class DelegateSmppSessionPolicy 
    {
        private readonly SmppSessionPolicyOptions _options;

        public DelegateSmppSessionPolicy(SmppSessionPolicyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<int> GetMaxInFlight(SmppSessionContext session)
        {
            if (_options.GetMaxInFlight is null)
                return 10;

            return await _options.GetMaxInFlight(session);

        }

        public async Task<bool> AllowMultipleBinds(string systemId)
        {
            if (_options.AllowMultipleBinds is null)
                return false;

            return await _options.AllowMultipleBinds(systemId);
        }

        public async Task<SmppPolicyResult> ValidateBind(
            SmppAuthContext authContext,
            SmppSessionContext session)
        {
            if (_options.ValidateBind is null)
                return SmppPolicyResult.Allow();

            return await _options.ValidateBind(authContext, session);
        }

        public async Task<SmppPolicyResult> ValidateSubmit(
            SmppSessionContext session,
            SmppSubmitRequest request)
        {
            if (_options.ValidateSubmit is null)
                return SmppPolicyResult.Allow();

            return await _options.ValidateSubmit(session, request);
        }


    }



}
