using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISmppSessionPolicy
    {

        /// <summary>
        /// For consumers to enforce backpressure; Core increments/decrements in-flight submits.
        /// </summary>
        Task<int> GetMaxInFlight(SmppSessionContext session);

        Task<bool> AllowMultipleBinds(string systemId);
        Task<SmppPolicyResult> ValidateBind(SmppAuthContext authContext,SmppSessionContext session);
        Task<SmppPolicyResult> ValidateSubmit( SmppSessionContext session, SmppSubmitRequest request );
    }

}
