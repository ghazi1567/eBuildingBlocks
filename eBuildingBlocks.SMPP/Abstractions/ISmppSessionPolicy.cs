using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISmppSessionPolicy
    {

        /// <summary>
        /// For consumers to enforce backpressure; Core increments/decrements in-flight submits.
        /// </summary>
        int GetMaxInFlight(SmppSessionContext session);

        bool AllowMultipleBinds(string systemId);
        SmppPolicyResult ValidateBind(SmppAuthContext authContext,SmppSessionContext session);
        SmppPolicyResult ValidateSubmit( SmppSessionContext session, SmppSubmitRequest request );
    }

}
