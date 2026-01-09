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
            _options = options;
        }

        public bool CanBind(string systemId)
        {
            return _options.CanBind?.Invoke(systemId) ?? true;
        }

        public bool CanSubmit(SmppSessionContext session)
        {
            return _options.CanSubmit?.Invoke(session) ?? true;
        }

        public int MaxInFlightPerSession
        {
            get { return _options.MaxInFlightPerSession ?? int.MaxValue; }
        }
    }


}
